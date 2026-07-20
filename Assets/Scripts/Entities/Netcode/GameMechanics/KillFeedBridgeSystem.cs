using System.Collections.Generic;
using Unity.Entities;

namespace Entities.Netcode.GameMechanics
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class KillfeedBridgeSystem : SystemBase
    {
        private readonly Dictionary<Entity, int> _localDeathCounts = new();

        protected override void OnUpdate()
        {
            if (KillfeedManager.Instance == null) return;

            foreach (var (pState, entity) in SystemAPI.Query<RefRO<PlayerStateComponent>>().WithEntityAccess())
            {
                if (!_localDeathCounts.TryGetValue(entity, out var lastSeenCount))
                {
                    _localDeathCounts[entity] = pState.ValueRO.DeathCount;
                    continue;
                }
                
                if (pState.ValueRO.DeathCount > lastSeenCount)
                {
                    _localDeathCounts[entity] = pState.ValueRO.DeathCount;
                    
                    if (pState.ValueRO.LastDeathReason != -1)
                        KillfeedManager.Instance.AddKillfeedEntry(
                            pState.ValueRO.LastKillerNetworkId,
                            pState.ValueRO.LastKillerTeamIndex,
                            pState.ValueRO.NetworkId,
                            pState.ValueRO.TeamIndex,
                            pState.ValueRO.LastDeathReason
                        );
                }
            }
        }
    }
}