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
                         .Query<RefRO<PlayerInput>, RefRO<LocalTransform>, RefRO<PlayerLook>, RefRW<PlayerStateComponent>>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                if (pState.ValueRO.IsDead) continue; 
                
                // --- 1. TICK COOLDOWNS & AUTO-RELOAD ---
                if (pState.ValueRO.PrimaryCooldownTimer > 0)
                {
                    pState.ValueRW.PrimaryCooldownTimer -= dt;
                    
                    // If the reload timer just finished and we are empty, refill the magazine!
                    if (pState.ValueRO.PrimaryCooldownTimer <= 0 && pState.ValueRO.CurrentAmmo <= 0)
                    {
                        pState.ValueRW.CurrentAmmo = entitiesReferences.MagazineSize;
                    }
                }
// --- NEW: TICK HITMARKER ---
                if (pState.ValueRO.HitMarkerTimer > 0)
                {
                    pState.ValueRW.HitMarkerTimer -= dt;
                }
                if (pState.ValueRO.SecondaryCooldownTimer > 0) pState.ValueRW.SecondaryCooldownTimer -= dt;
                if (pState.ValueRO.FourthCooldownTimer > 0) pState.ValueRW.FourthCooldownTimer -= dt;
                
                // --- 2. READ INPUTS ---
                bool isPrimaryHeld = input.ValueRO.PrimaryAbilityInput;
                bool isSecondaryPressed = input.ValueRO.SecondaryAbilityInput;
                bool wasSecondaryPressed = pState.ValueRO.PreviousSecondaryInput;
                bool isFourthPressed = input.ValueRO.FourthAbilityInput;
                bool wasFourthPressed = pState.ValueRO.PreviousFourthInput;

                // --- 3. HANDLE EMPTY MAG ON SPAWN ---
                // If they hold fire, have no cooldown, but the gun is empty, force a reload to start.
                if (pState.ValueRO.PrimaryCooldownTimer <= 0 && pState.ValueRO.CurrentAmmo <= 0)
                {
                    pState.ValueRW.CurrentAmmo = entitiesReferences.MagazineSize;
                    // --- NEW: RELOAD SOUND ---
                    if (networkTime.IsFirstTimeFullyPredictingTick)
                    {
                        var audioReq = ecb.CreateEntity();
                        ecb.AddComponent(audioReq, new AudioRequest { Effect = SFX.Reload, Position = transform.ValueRO.Position, Pitch = 1f, LocalTargetNetworkId = -1 });
                    }
                }

                bool primaryTriggered = isPrimaryHeld && pState.ValueRO.PrimaryCooldownTimer <= 0 && pState.ValueRO.CurrentAmmo > 0;
                bool secondaryTriggered = isSecondaryPressed && !wasSecondaryPressed && pState.ValueRO.SecondaryCooldownTimer <= 0;

                // --- 4. EXECUTE PRIMARY (SHOOT) ---
                if (primaryTriggered)
                {
                    // A. Update Prediction State EVERY TICK
                    pState.ValueRW.CurrentAmmo--;

                    if (pState.ValueRO.CurrentAmmo <= 0)
                    {
                        pState.ValueRW.PrimaryCooldownTimer = entitiesReferences.ReloadTime; // Start Reload
                        // --- NEW: RELOAD SOUND ---
                        if (networkTime.IsFirstTimeFullyPredictingTick)
                        {
                            var audioReq = ecb.CreateEntity();
                            ecb.AddComponent(audioReq, new AudioRequest { Effect = SFX.Reload, Position = transform.ValueRO.Position, Pitch = 1f, LocalTargetNetworkId = -1 });
                        }
                    }
                    else
                    {
                        pState.ValueRW.PrimaryCooldownTimer = entitiesReferences.FireRate; // Standard Fire Rate
                    }

                    // B. Spawn visual/network requests ONLY on forward ticks
                    if (networkTime.IsFirstTimeFullyPredictingTick)
                    {
                        
                        var aimRotation = math.mul(quaternion.RotateY(look.ValueRO.Yaw), quaternion.RotateX(-look.ValueRO.Pitch));
                        var aimDirection = math.mul(aimRotation, new float3(0, 0, 1));
                        var spawnOrigin = transform.ValueRO.Position + new float3(0, 1.8f, 0); 
                        var audioReq = ecb.CreateEntity();
                        ecb.AddComponent(audioReq, new AudioRequest { Effect = SFX.FireHitscan, Position = spawnOrigin, Pitch = 1f, LocalTargetNetworkId = -1 });

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

                // --- 5. EXECUTE SECONDARY ---
                if (secondaryTriggered)
                {
                    // Update state every tick
                    pState.ValueRW.SecondaryCooldownTimer = entitiesReferences.secondaryCooldown;

                    if (networkTime.IsFirstTimeFullyPredictingTick)
                    {
                        var aimRotation = math.mul(quaternion.RotateY(look.ValueRO.Yaw), quaternion.RotateX(-look.ValueRO.Pitch));
                        var aimDirection = math.mul(aimRotation, new float3(0, 0, 1));
                        var spawnOrigin = transform.ValueRO.Position + new float3(0, 1.8f, 0); 

                        var reqEntity = ecb.CreateEntity();
                        ecb.AddComponent(reqEntity, new ProjectileSpawnRequest
                        {
                            Owner = entity,
                            Origin = spawnOrigin,
                            Direction = aimDirection,
                            Speed = 1f,             
                            Damage = 35f,
                            Lifespan = 10f,
                            ApplyGravity = false,
                            Radius = 0.2f
                        });
                        var audioReq = ecb.CreateEntity();
                        ecb.AddComponent(audioReq, new AudioRequest { Effect = SFX.FireProj, Position = spawnOrigin, Pitch = 1f, LocalTargetNetworkId = -1 });
                    }
                  
                    
                }

                // --- 6. EXECUTE FOURTH (DEBUG SUICIDE) ---
                if (isFourthPressed && !wasFourthPressed && pState.ValueRO.FourthCooldownTimer <= 0)
                {
                    pState.ValueRW.FourthCooldownTimer = 15.0f;
                    pState.ValueRW.Health = 0;
                    pState.ValueRW.LastDeathReason = 3;
                    pState.ValueRW.LastKillerTeamIndex = pState.ValueRO.TeamIndex;
                    pState.ValueRW.LastKillerNetworkId = pState.ValueRO.NetworkId;
                }
                
                // --- 7. SAVE INPUTS FOR EDGE DETECTION ---
                pState.ValueRW.PreviousPrimaryInput = isPrimaryHeld;
                pState.ValueRW.PreviousSecondaryInput = isSecondaryPressed;
                pState.ValueRW.PreviousFourthInput = isFourthPressed;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }    }
}