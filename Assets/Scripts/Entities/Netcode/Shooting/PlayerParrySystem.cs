using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Entities.Netcode.Abilities
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerParrySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EntitiesReferences>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
            var dt = SystemAPI.Time.DeltaTime;
            var isServer = state.WorldUnmanaged.IsServer();

            foreach (var (input, pState, entity) in SystemAPI
                         .Query<RefRO<PlayerInput>, RefRW<PlayerStateComponent>>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                if (pState.ValueRO.IsDead) continue; // <--- Blocks all shooting and abilities
                // 1. Tick down cooldown
                if (pState.ValueRO.ThirdCooldownTimer > 0)
                    pState.ValueRW.ThirdCooldownTimer -= dt;

                bool isPressed = input.ValueRO.ThirdAbilityInput;
                bool wasPressed = pState.ValueRO.PreviousThirdInput;
                
                // 2. Gate trigger by cooldown
                bool triggered = isPressed && !wasPressed && pState.ValueRO.ThirdCooldownTimer <= 0;

                bool shouldLog = isServer || networkTime.IsFirstTimeFullyPredictingTick;
                string role = isServer ? "Server" : "Client";

                if (pState.ValueRO.ParryTimer > 0)
                {
                    pState.ValueRW.ParryTimer -= dt;
                    if (pState.ValueRO.ParryTimer <= 0)
                    {
                        pState.ValueRW.IsParrying = false;
                        if (shouldLog) 
                            UnityEngine.Debug.Log($"[{role}] Parry ENDED for Player {entity.Index}");
                    }
                }
                else if (triggered)
                {
                    pState.ValueRW.IsParrying = true;
                    pState.ValueRW.ParryTimer = 0.5f; 
                    
                    // 3. Reset Cooldown Timer
                    pState.ValueRW.ThirdCooldownTimer = entitiesReferences.thirdCooldown;
                    
                    if (shouldLog) 
                        UnityEngine.Debug.Log($"[{role}] Parry BEGAN for Player {entity.Index}");
                }

                pState.ValueRW.PreviousThirdInput = isPressed;
            }
        }
    }
}