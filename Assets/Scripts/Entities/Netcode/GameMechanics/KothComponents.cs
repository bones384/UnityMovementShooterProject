using Unity.Entities;
using Unity.NetCode;

namespace Entities.Netcode.GameMechanics
{
    [GhostComponent(PrefabType = GhostPrefabType.All)]
    public struct KothPointComponent : IComponentData
    {
        public float Radius;
        public float Height;
        public float TimeToWin;
        public float TimeToCapture;
        
        [GhostField] public float TeamATimer;
        [GhostField] public float TeamBTimer;
        [GhostField] public int CurrentOwner;
        [GhostField] public int CapturingTeam;
        [GhostField] public float CaptureProgress;
        [GhostField] public bool IsOvertime;
        
        [GhostField] public bool IsContested;
        [GhostField] public int CappingPlayerCount;

        [GhostField] public bool IsGameOver;
        [GhostField] public int WinningTeam;
        
        public float RestartTimer;
    }
}