using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

namespace Entities.Netcode.GameMechanics
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(PlayerDeathRespawnSystem))]
    public partial struct KillzoneSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<KillzoneTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

            var job = new KillzoneTriggerJob
            {
                PlayerStateLookup = SystemAPI.GetComponentLookup<PlayerStateComponent>(),
                KillzoneLookup = SystemAPI.GetComponentLookup<KillzoneTag>(true)
            };

            state.Dependency = job.Schedule(simulation, state.Dependency);
        }
    }
    
    public struct KillzoneTriggerJob : ITriggerEventsJob
    {
        public ComponentLookup<PlayerStateComponent> PlayerStateLookup;
        [ReadOnly] public ComponentLookup<KillzoneTag> KillzoneLookup;

        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.EntityA;
            var entityB = triggerEvent.EntityB;

            var isAKillzone = KillzoneLookup.HasComponent(entityA);
            var isBKillzone = KillzoneLookup.HasComponent(entityB);
            var isAPlayer = PlayerStateLookup.HasComponent(entityA);
            var isBPlayer = PlayerStateLookup.HasComponent(entityB);
            
            if (isAKillzone || isBKillzone)
            {
                Debug.Log(
                    $"[Physics] Killzone trigger overlapped with Entity! (A: {entityA.Index}, B: {entityB.Index})");
                Debug.Log($"[Physics] Is A Player? {isAPlayer} | Is B Player? {isBPlayer}");
            }

            if (isAKillzone && isBPlayer)
                KillPlayer(entityB);
            else if (isBKillzone && isAPlayer) KillPlayer(entityA);
        }

        private void KillPlayer(Entity playerEntity)
        {
            var pState = PlayerStateLookup[playerEntity];

            if (pState.Health > 0f && !pState.IsDead)
            {
                Debug.Log($"[Killzone] Executing Player {playerEntity.Index}!");
                pState.Health = 0f;
                
                pState.LastKillerNetworkId = -1;
                pState.LastDeathReason = 2;
                pState.LastKillerTeamIndex = -1;

                PlayerStateLookup[playerEntity] = pState;
            }
        }
    }
}