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

        // Live State
        [GhostField] public float TeamATimer;
        [GhostField] public float TeamBTimer;
        [GhostField] public int CurrentOwner;    // -1 = Neutral, 0 = Team A, 1 = Team B
        [GhostField] public int CapturingTeam;   // -1 = Neutral, 0 = A, 1 = B
        [GhostField] public float CaptureProgress; 
        [GhostField] public bool IsOvertime;
        
        // --- NEW TF2 STATE FIELDS ---
        [GhostField] public bool IsContested;    // True if both teams are on point
        [GhostField] public int CappingPlayerCount; // Number of players actively capping
        
        [GhostField] public bool IsGameOver;
        [GhostField] public int WinningTeam;
        
        // This doesn't need to be ghosted, only the server needs to count down the restart
        public float RestartTimer;
    }
}