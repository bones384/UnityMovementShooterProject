using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using SphereCollider = Unity.Physics.SphereCollider;
using Collider = Unity.Physics.Collider;

namespace Entities.Netcode.Shooting
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct ProjectileSimulationSystem : ISystem
    {
        private static float3 ClosestPointOnSegment(float3 a, float3 b, float3 p)
        {
            float3 ab = b - a;
            float lenSq = math.lengthsq(ab);
            if (lenSq < 1e-5f) return a; 
            
            float t = math.saturate(math.dot(p - a, ab) / lenSq);
            return a + t * ab;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<EntitiesReferences>();
        }

        public unsafe void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var gravityMagnitude = math.abs(SystemAPI.GetSingleton<EntitiesReferences>().Gravity);
            var isServer = state.WorldUnmanaged.IsServer();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            float parryRange = 4.0f;
            float parryAngle = 45f; 
            float parrySpeedMult = 2.0f;
            float parryDamageMult = 2.0f;

            foreach (var (projectile, transform, entity) in SystemAPI
                         .Query<RefRW<ProjectileComponent>, RefRW<LocalTransform>>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                projectile.ValueRW.Lifespan -= dt;
                
                if (projectile.ValueRO.Lifespan <= 0)
                {
                    if (isServer)
                    {
                        ecb.DestroyEntity(entity);
                    }
                    else
                    {
                        transform.ValueRW.Position = new float3(0, -9999f, 0);
                        transform.ValueRW.Scale = 0f;
                    }
                    continue;
                }

                if (projectile.ValueRO.ApplyGravity)
                {
                    projectile.ValueRW.Velocity.y -= gravityMagnitude * dt;
                }

                var currentPos = transform.ValueRO.Position;
                var displacement = projectile.ValueRO.Velocity * dt;
                
                bool shouldLog = isServer || networkTime.IsFirstTimeFullyPredictingTick;
                string role = isServer ? "Server" : "Client";
                bool wasDeflectedThisFrame = false;

                foreach (var (pState, pLook, pTransform, pEntity) in SystemAPI
                             .Query<RefRO<PlayerStateComponent>, RefRO<PlayerLook>, RefRO<LocalTransform>>()
                             .WithEntityAccess())
                {
                    bool isSameTeam = false;
                    if (projectile.ValueRO.Owner != Entity.Null && SystemAPI.HasComponent<PlayerStateComponent>(projectile.ValueRO.Owner))
                    {
                        var ownerState = SystemAPI.GetComponent<PlayerStateComponent>(projectile.ValueRO.Owner);
                        isSameTeam = pState.ValueRO.TeamIndex == ownerState.TeamIndex;
                    }

                    // Use the safe boolean here!
                    if (!pState.ValueRO.IsParrying || pEntity == projectile.ValueRO.Owner || isSameTeam) 
                        continue;

                    float3 eyePos = pTransform.ValueRO.Position + new float3(0, 1.8f, 0);
                    
                    var aimRot = math.mul(quaternion.RotateY(pLook.ValueRO.Yaw), quaternion.RotateX(-pLook.ValueRO.Pitch));
                    var aimDir = math.mul(aimRot, new float3(0, 0, 1));

                    float3 closestPoint = ClosestPointOnSegment(currentPos, currentPos + displacement, eyePos);
                    float3 toProj = closestPoint - eyePos;
                    float distSq = math.lengthsq(toProj);

                    if (distSq < parryRange * parryRange)
                    {
                        float dot = math.dot(math.normalizesafe(toProj), aimDir);
                        if (dot > math.cos(math.radians(parryAngle)))
                        {
                            projectile.ValueRW.Velocity = aimDir * math.length(projectile.ValueRO.Velocity) * parrySpeedMult;
                            projectile.ValueRW.Damage *= parryDamageMult;
                            projectile.ValueRW.Owner = pEntity; 
                            
                            transform.ValueRW.Rotation = quaternion.LookRotationSafe(projectile.ValueRO.Velocity, math.up());
                            projectile.ValueRW.Lifespan += 10;
    
                            wasDeflectedThisFrame = true;
                            if (shouldLog) UnityEngine.Debug.Log($"[{role}] VOLUME DEFLECT!");
                            
                            float speedPitch = math.clamp(math.length(projectile.ValueRO.Velocity) * 0.05f, 1f, 3f);
                            if (networkTime.IsFirstTimeFullyPredictingTick)
                            {
                                var audioReq = ecb.CreateEntity();
                                ecb.AddComponent(audioReq, new AudioRequest { Effect = SFX.Parry, Position = currentPos, Pitch = speedPitch, LocalTargetNetworkId = -1 });
                            }
                            
                            break;
                        }
                    }
                }

                if (wasDeflectedThisFrame) continue;

                var filter = new CollisionFilter
                {
                    BelongsTo = 1u << 7, 
                    CollidesWith = (1u << 6) | (1u << 7), 
                    GroupIndex = 0
                };

                var sphereGeom = new SphereGeometry { Center = float3.zero, Radius = projectile.ValueRO.Radius };
                var sphereCollider = SphereCollider.Create(sphereGeom, filter);
                var colliderPtr = (Collider*)sphereCollider.GetUnsafePtr();

                var castInput = new ColliderCastInput
                {
                    Collider = colliderPtr,
                    Orientation = transform.ValueRO.Rotation,
                    Start = currentPos,
                    End = currentPos + displacement
                };

                var collector = new IgnoreOwnerColliderCollector(projectile.ValueRO.Owner);
                collisionWorld.CastCollider(castInput, ref collector);

                bool hit = collector.NumHits > 0;
                var hitResult = collector.ClosestHit;

                if (hit)
                {
                    bool destroyProjectile = true;

                    if (SystemAPI.HasComponent<PlayerStateComponent>(hitResult.Entity))
                    {
                        var targetState = SystemAPI.GetComponent<PlayerStateComponent>(hitResult.Entity);
                        
                        if (targetState.IsParrying)
                        {
                            var targetLook = SystemAPI.GetComponent<PlayerLook>(hitResult.Entity);
                            var aimRot = math.mul(quaternion.RotateY(targetLook.Yaw), quaternion.RotateX(-targetLook.Pitch));
                            var aimDir = math.mul(aimRot, new float3(0, 0, 1));

                            projectile.ValueRW.Velocity = aimDir * math.length(projectile.ValueRO.Velocity) * parrySpeedMult;
                            projectile.ValueRW.Damage *= parryDamageMult;
                            projectile.ValueRW.Owner = hitResult.Entity;
                            projectile.ValueRW.Lifespan += 10;
                            // Visual update only: keep the exact same position, just rotate the mesh
                            transform.ValueRW.Rotation = quaternion.LookRotationSafe(projectile.ValueRO.Velocity, math.up());
                            destroyProjectile = false; 
                            float speedPitch = math.clamp(math.length(projectile.ValueRO.Velocity) * 0.05f, 1f, 3f);
                            if (networkTime.IsFirstTimeFullyPredictingTick)
                            {
                                var audioReq = ecb.CreateEntity();
                                ecb.AddComponent(audioReq, new AudioRequest { Effect = SFX.Parry, Position = currentPos, Pitch = speedPitch, LocalTargetNetworkId = -1 });
                            }
                            if (shouldLog) UnityEngine.Debug.Log($"[{role}] BODY DEFLECT!");
                        }
                        else
                        {
                            if (shouldLog) UnityEngine.Debug.Log($"[{role}] Projectile HIT Player!");
                            // --- NEW SAFE DAMAGE CHECK ---
                            bool isEnemy = true; // Default to true so orphaned projectiles still deal damage
                            if (projectile.ValueRO.Owner != Entity.Null && SystemAPI.HasComponent<PlayerStateComponent>(projectile.ValueRO.Owner))
                            {
                                var ownerState = SystemAPI.GetComponent<PlayerStateComponent>(projectile.ValueRO.Owner);
                                isEnemy = targetState.TeamIndex != ownerState.TeamIndex;
                            }

                            if (isEnemy)
                            {
                                targetState.Health -= projectile.ValueRO.Damage;
                                // Record Projectile Death
                                if (targetState.Health <= 0)
                                {
                                    targetState.LastDeathReason = 1; // Projectile
                                    
                                    if (projectile.ValueRO.Owner != Entity.Null && SystemAPI.HasComponent<PlayerStateComponent>(projectile.ValueRO.Owner))
                                    {
                                        targetState.LastKillerNetworkId = SystemAPI
                                            .GetComponent<PlayerStateComponent>(projectile.ValueRO.Owner).NetworkId;
                                        targetState.LastKillerTeamIndex = SystemAPI
                                            .GetComponent<PlayerStateComponent>(projectile.ValueRO.Owner).TeamIndex;
                                    }                                    else
                                    {
                                        targetState.LastKillerNetworkId = -1; // Orphaned projectile
                                        targetState.LastKillerTeamIndex = -1;
                                    }
       
                                }                        // --- NEW: HITMARKER LOGIC ---
                                // Find the shooter and give them a hitmarker
                                if (projectile.ValueRO.Owner != Entity.Null && SystemAPI.HasComponent<PlayerStateComponent>(projectile.ValueRO.Owner))
                                {
                                    var ownerState = SystemAPI.GetComponent<PlayerStateComponent>(projectile.ValueRO.Owner);
                                    ownerState.HitMarkerTimer = 0.2f;
                                    ownerState.WasLastHitFatal = targetState.Health <= 0;
                                    SystemAPI.SetComponent(projectile.ValueRO.Owner, ownerState);
                                }
                                if (networkTime.IsFirstTimeFullyPredictingTick && projectile.ValueRO.Owner != Entity.Null && SystemAPI.HasComponent<PlayerStateComponent>(projectile.ValueRO.Owner))
                                {
                                    var shooterId = SystemAPI.GetComponent<PlayerStateComponent>(projectile.ValueRO.Owner).NetworkId;
                                    var audioReq = ecb.CreateEntity();
                                    // Hit Confirm is Local Only!
                                    ecb.AddComponent(audioReq, new AudioRequest { Effect = SFX.HitConfirm, Pitch = 1f, LocalTargetNetworkId = shooterId });
                                }
                            }
                            
                            SystemAPI.SetComponent(hitResult.Entity, targetState);

                        }
                    }
                    else
                    {
                        if (shouldLog) UnityEngine.Debug.Log($"[{role}] Projectile HIT Wall");
                        if (networkTime.IsFirstTimeFullyPredictingTick)
                        {
                            var audioReq = ecb.CreateEntity();
                            ecb.AddComponent(audioReq, new AudioRequest { Effect = SFX.HitGround, Position = currentPos, Pitch = 1f, LocalTargetNetworkId = -1 });
                        }
                    }

                    if (destroyProjectile)
                    {
                        if (isServer)
                        {
                            ecb.DestroyEntity(entity);
                        }
                        else
                        {
                            transform.ValueRW.Position = new float3(0, -9999f, 0);
                            transform.ValueRW.Scale = 0f;
                            projectile.ValueRW.Velocity = float3.zero;
                            projectile.ValueRW.Lifespan = 0f; 
                        }
                    }
                }
                else
                {
                    transform.ValueRW.Position += displacement;
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(projectile.ValueRO.Velocity, math.up());
                }

                sphereCollider.Dispose();
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}