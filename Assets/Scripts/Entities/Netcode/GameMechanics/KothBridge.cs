using Unity.Entities;
using Unity.NetCode;

namespace Entities.Netcode.GameMechanics
{
    public static class KothBridge
    {
        public static bool IsActive;
        public static KothPointComponent State;
        public static bool IsGameOver;
        public static int WinningTeam;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct KothBridgeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            bool found = false;

            foreach (var koth in SystemAPI.Query<RefRO<KothPointComponent>>())
            {
                KothBridge.State = koth.ValueRO;
                KothBridge.IsGameOver = koth.ValueRO.IsGameOver;
                KothBridge.WinningTeam = koth.ValueRO.WinningTeam;
                found = true;
                break; 
            }

            KothBridge.IsActive = found;
        }
    }
}