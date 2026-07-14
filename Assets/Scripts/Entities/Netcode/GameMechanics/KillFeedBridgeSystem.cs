using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;

namespace Entities.Netcode.GameMechanics
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class KillfeedBridgeSystem : SystemBase
    {
        private Dictionary<Entity, int> _localDeathCounts = new Dictionary<Entity, int>();

        protected override void OnUpdate()
        {
            if (KillfeedManager.Instance == null) return;

            foreach (var (pState, entity) in SystemAPI.Query<RefRO<PlayerStateComponent>>().WithEntityAccess())
            {
                // First time seeing this entity, just record its current count
                if (!_localDeathCounts.TryGetValue(entity, out int lastSeenCount))
                {
                    _localDeathCounts[entity] = pState.ValueRO.DeathCount;
                    continue;
                }

                // If the server's count is higher than our local count, a death happened!
                if (pState.ValueRO.DeathCount > lastSeenCount)
                {
                    _localDeathCounts[entity] = pState.ValueRO.DeathCount;

                    // -1 means game restart, so we silently skip it
                    if (pState.ValueRO.LastDeathReason != -1)
                    {
                        KillfeedManager.Instance.AddKillfeedEntry(
                            pState.ValueRO.LastKillerNetworkId,
                            pState.ValueRO.LastKillerTeamIndex, // <-- Killer Team
                            pState.ValueRO.NetworkId,
                            pState.ValueRO.TeamIndex,           // <-- Victim Team
                            pState.ValueRO.LastDeathReason
                        );
                    }
                }
            }
        }
    }
}