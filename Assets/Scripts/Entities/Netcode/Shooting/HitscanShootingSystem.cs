using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Entities.Netcode.Shooting
{
    public struct IgnoreOwnerCollector : ICollector<RaycastHit>
    {
        public Entity IgnoreEntity;

        // We cannot early out because the first hit might be the owner we want to ignore.
        // We must evaluate everything along the ray.
        public bool EarlyOutOnFirstHit => false;

        public float MaxFraction { get; private set; }
        public int NumHits { get; private set; }
        public RaycastHit ClosestHit;

        public IgnoreOwnerCollector(Entity ignoreEntity)
        {
            IgnoreEntity = ignoreEntity;
            MaxFraction = 1f; // 1.0 means the very end of the ray
            NumHits = 0;
            ClosestHit = default;
        }

        public bool AddHit(RaycastHit hit)
        {
            // If the hit is the entity we are trying to ignore, discard it.
            if (hit.Entity == IgnoreEntity)
                return false;

            // If it's a valid entity, save it as our new closest hit
            // and shrink the MaxFraction so we only evaluate things closer than this.
            MaxFraction = hit.Fraction;
            ClosestHit = hit;
            NumHits = 1;
            return true;
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct HitscanShootingSystem : ISystem
    {
        private float3 lastStart;
        private float3 lastEnd;
        private bool hasLine;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();

            if (!networkTime.IsFirstTimeFullyPredictingTick)
                return;

            var isServer = state.WorldUnmanaged.IsServer();
            var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var physicsWorld = physicsWorldSingleton.PhysicsWorld;
            var defaultCollisionWorld = physicsWorldSingleton.CollisionWorld;

            var hasHistory = SystemAPI.TryGetSingleton<PhysicsWorldHistorySingleton>(out var collisionHistory);

            var delayLookup = SystemAPI.GetComponentLookup<CommandDataInterpolationDelay>(true);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, entity) in SystemAPI.Query<RefRO<HitscanBulletRequest>>().WithEntityAccess())
            {
                var collisionWorld = defaultCollisionWorld;

                if (isServer && hasHistory)
                {
                    const uint additionalRenderDelay = 1;
                    var delay = additionalRenderDelay;

                    delayLookup.TryGetComponent(request.ValueRO.Owner, out var interpDelay);
                    {
                        delay = interpDelay.Delay + additionalRenderDelay;
                    }

                    collisionHistory.GetCollisionWorldFromTick(
                        request.ValueRO.Tick,
                        delay,
                        ref physicsWorld,
                        out collisionWorld,
                        out var expectedTick,
                        out var returnedTick);
                }

                var origin = request.ValueRO.Origin;
                var direction = request.ValueRO.Direction;
                var distance = request.ValueRO.Range;

                var input = new RaycastInput
                {
                    Start = origin,
                    End = origin + direction * distance,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = 1u << 7,
                        CollidesWith = (1u << 6) | (1u << 7),
                        GroupIndex = 0
                    }
                };

                var collector = new IgnoreOwnerCollector(request.ValueRO.Owner);
                collisionWorld.CastRay(input, ref collector);

                var hit = collector.NumHits > 0;
                var hitResult = collector.ClosestHit;

                var targetEnd = origin + direction * distance;
                var role = isServer ? "Server" : "Client";

                if (hit)
                {
                    targetEnd = hitResult.Position;

                    if (SystemAPI.HasComponent<PlayerStateComponent>(hitResult.Entity))
                    {
                        var pState = SystemAPI.GetComponent<PlayerStateComponent>(hitResult.Entity);
                        var ownerState = SystemAPI.GetComponent<PlayerStateComponent>(request.ValueRO.Owner);
                        var isSameTeam = pState.TeamIndex == ownerState.TeamIndex;
                        if (!isSameTeam)
                        {
                            pState.Health -= request.ValueRO.Damage;
                            // Record Killzone Death
                            if (pState.Health <= 0)
                            {
                                pState.LastKillerNetworkId = ownerState.NetworkId;
                                pState.LastDeathReason = 0;
                                pState.LastKillerTeamIndex = ownerState.TeamIndex; // <-- NEW
                            }

                            // --- NEW: HITMARKER LOGIC ---
                            // Find the shooter and give them a hitmarker
                            ownerState.HitMarkerTimer = 0.2f;
                            ownerState.WasLastHitFatal = pState.Health <= 0;
                            SystemAPI.SetComponent(request.ValueRO.Owner, ownerState);
                        }

                        SystemAPI.SetComponent(hitResult.Entity, pState);


                        Debug.Log($"[{role}] HIT Player! Target Health is now: {pState.Health}");
                    }
                    else
                    {
                        Debug.Log($"[{role}] HIT Environment at {targetEnd}");
                        var audioReq = ecb.CreateEntity();
                        ecb.AddComponent(audioReq,
                            new AudioRequest
                            {
                                Effect = SFX.HitGround, Position = targetEnd, Pitch = 1f, LocalTargetNetworkId = -1
                            });
                    }
                }
                else
                {
                    Debug.Log($"[{role}] MISS!");
                    var audioReq = ecb.CreateEntity();
                    ecb.AddComponent(audioReq,
                        new AudioRequest
                            { Effect = SFX.HitGround, Position = targetEnd, Pitch = 1f, LocalTargetNetworkId = -1 });
                }

                lastStart = origin;
                lastEnd = targetEnd;
                hasLine = true;

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            if (hasLine)
            {
                var color = isServer ? Color.red : Color.blue;
                Debug.DrawLine(lastStart, lastEnd, color);
            }
        }
    }
}