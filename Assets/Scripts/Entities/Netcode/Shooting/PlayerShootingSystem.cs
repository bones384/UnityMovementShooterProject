using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Entities.Netcode.Shooting
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(HitscanShootingSystem))]
    [UpdateBefore(typeof(ProjectileSpawnerSystem))]
    public partial struct PlayerShootingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EntitiesReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
            var dt = SystemAPI.Time.DeltaTime;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (input, transform, look, pState, entity) in SystemAPI
                         .Query<RefRO<PlayerInput>, RefRO<LocalTransform>, RefRO<PlayerLook>,
                             RefRW<PlayerStateComponent>>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                if (pState.ValueRO.IsDead) continue;
                
                if (pState.ValueRO.PrimaryCooldownTimer > 0)
                {
                    pState.ValueRW.PrimaryCooldownTimer -= dt;
                    
                    if (pState.ValueRO.PrimaryCooldownTimer <= 0 && pState.ValueRO.CurrentAmmo <= 0)
                        pState.ValueRW.CurrentAmmo = entitiesReferences.MagazineSize;
                }
                
                if (pState.ValueRO.HitMarkerTimer > 0) pState.ValueRW.HitMarkerTimer -= dt;
                if (pState.ValueRO.SecondaryCooldownTimer > 0) pState.ValueRW.SecondaryCooldownTimer -= dt;
                if (pState.ValueRO.FourthCooldownTimer > 0) pState.ValueRW.FourthCooldownTimer -= dt;
                
                var isPrimaryHeld = input.ValueRO.PrimaryAbilityInput;
                var isSecondaryPressed = input.ValueRO.SecondaryAbilityInput;
                var wasSecondaryPressed = pState.ValueRO.PreviousSecondaryInput;
                var isFourthPressed = input.ValueRO.FourthAbilityInput;
                var wasFourthPressed = pState.ValueRO.PreviousFourthInput;
                
                if (pState.ValueRO.PrimaryCooldownTimer <= 0 && pState.ValueRO.CurrentAmmo <= 0)
                {
                    pState.ValueRW.CurrentAmmo = entitiesReferences.MagazineSize;
                    if (networkTime.IsFirstTimeFullyPredictingTick)
                    {
                        var audioReq = ecb.CreateEntity();
                        ecb.AddComponent(audioReq,
                            new AudioRequest
                            {
                                Effect = SFX.Reload, Position = transform.ValueRO.Position, Pitch = 1f,
                                LocalTargetNetworkId = -1
                            });
                    }
                }

                var primaryTriggered = isPrimaryHeld && pState.ValueRO.PrimaryCooldownTimer <= 0 &&
                                       pState.ValueRO.CurrentAmmo > 0;
                var secondaryTriggered = isSecondaryPressed && !wasSecondaryPressed &&
                                         pState.ValueRO.SecondaryCooldownTimer <= 0;
                
                if (primaryTriggered)
                {
                    pState.ValueRW.CurrentAmmo--;

                    if (pState.ValueRO.CurrentAmmo <= 0)
                    {
                        pState.ValueRW.PrimaryCooldownTimer = entitiesReferences.ReloadTime;
                        if (networkTime.IsFirstTimeFullyPredictingTick)
                        {
                            var audioReq = ecb.CreateEntity();
                            ecb.AddComponent(audioReq,
                                new AudioRequest
                                {
                                    Effect = SFX.Reload, Position = transform.ValueRO.Position, Pitch = 1f,
                                    LocalTargetNetworkId = -1
                                });
                        }
                    }
                    else
                    {
                        pState.ValueRW.PrimaryCooldownTimer = entitiesReferences.FireRate;
                    }
                    
                    if (networkTime.IsFirstTimeFullyPredictingTick)
                    {
                        var aimRotation = math.mul(quaternion.RotateY(look.ValueRO.Yaw),
                            quaternion.RotateX(-look.ValueRO.Pitch));
                        var aimDirection = math.mul(aimRotation, new float3(0, 0, 1));
                        var spawnOrigin = transform.ValueRO.Position + new float3(0, 1.8f, 0);
                        var audioReq = ecb.CreateEntity();
                        ecb.AddComponent(audioReq,
                            new AudioRequest
                            {
                                Effect = SFX.FireHitscan, Position = spawnOrigin, Pitch = 1f, LocalTargetNetworkId = -1
                            });

                        var reqEntity = ecb.CreateEntity();
                        ecb.AddComponent(reqEntity, new HitscanBulletRequest
                        {
                            Owner = entity,
                            Damage = 5f,
                            Range = 100f,
                            Origin = spawnOrigin,
                            Direction = aimDirection,
                            Tick = networkTime.ServerTick
                        });
                    }
                }
                
                if (secondaryTriggered)
                {
                    pState.ValueRW.SecondaryCooldownTimer = entitiesReferences.secondaryCooldown;

                    if (networkTime.IsFirstTimeFullyPredictingTick)
                    {
                        var aimRotation = math.mul(quaternion.RotateY(look.ValueRO.Yaw),
                            quaternion.RotateX(-look.ValueRO.Pitch));
                        var aimDirection = math.mul(aimRotation, new float3(0, 0, 1));
                        var spawnOrigin = transform.ValueRO.Position + new float3(0, 1.8f, 0);

                        var reqEntity = ecb.CreateEntity();
                        ecb.AddComponent(reqEntity, new ProjectileSpawnRequest
                        {
                            Owner = entity,
                            Origin = spawnOrigin,
                            Direction = aimDirection,
                            Speed = 10f,
                            Damage = 35f,
                            Lifespan = 10f,
                            ApplyGravity = false,
                            Radius = 0.2f
                        });
                        var audioReq = ecb.CreateEntity();
                        ecb.AddComponent(audioReq,
                            new AudioRequest
                            {
                                Effect = SFX.FireProj, Position = spawnOrigin, Pitch = 1f, LocalTargetNetworkId = -1
                            });
                    }
                }
                
                if (isFourthPressed && !wasFourthPressed && pState.ValueRO.FourthCooldownTimer <= 0)
                {
                    pState.ValueRW.FourthCooldownTimer = 15.0f;
                    pState.ValueRW.Health = 0;
                    pState.ValueRW.LastDeathReason = 3;
                    pState.ValueRW.LastKillerTeamIndex = pState.ValueRO.TeamIndex;
                    pState.ValueRW.LastKillerNetworkId = pState.ValueRO.NetworkId;
                }
                
                pState.ValueRW.PreviousPrimaryInput = isPrimaryHeld;
                pState.ValueRW.PreviousSecondaryInput = isSecondaryPressed;
                pState.ValueRW.PreviousFourthInput = isFourthPressed;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}