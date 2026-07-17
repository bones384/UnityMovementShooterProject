using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Entities.Netcode.GameMechanics
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct KothLogicSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (koth, kothTransform) in SystemAPI
                         .Query<RefRW<KothPointComponent>, RefRO<LocalTransform>>())
            {
                // --- GAME OVER & RESTART LOGIC ---
                if (koth.ValueRO.IsGameOver)
                {
                    koth.ValueRW.RestartTimer -= dt;
                    if (koth.ValueRO.RestartTimer <= 0)
                    {
                        koth.ValueRW.IsGameOver = false;
                        koth.ValueRW.WinningTeam = -1;
                        koth.ValueRW.CurrentOwner = -1;
                        koth.ValueRW.CapturingTeam = -1;
                        koth.ValueRW.CaptureProgress = 0f;
                        koth.ValueRW.TeamATimer = koth.ValueRO.TimeToWin;
                        koth.ValueRW.TeamBTimer = koth.ValueRO.TimeToWin;

                        // Kill all players to trigger the standard respawn pipeline instantly
                        foreach (var pState in SystemAPI.Query<RefRW<PlayerStateComponent>>())
                        {
                            pState.ValueRW.Health = 0f;
                            pState.ValueRW.IsDead = true;
                            pState.ValueRW.RespawnTimer = 0f; // <--- Instant respawn override!
                            pState.ValueRW.LastKillerTeamIndex = -1; // <-- NEW
                            // SILENT DEATH FOR RESTART
                            pState.ValueRW.LastDeathReason = -1; 
                            pState.ValueRW.DeathCount++;
                        }
                    }
                    continue; // Skip all capture logic while game is over
                }

                int countA = 0;
                int countB = 0;
                float radiusSq = koth.ValueRO.Radius * koth.ValueRO.Radius;
                float halfHeight = koth.ValueRO.Height * 0.5f; 
                float3 kothPos = kothTransform.ValueRO.Position;

                foreach (var (pState, pTransform) in SystemAPI.Query<RefRO<PlayerStateComponent>, RefRO<LocalTransform>>())
                {
                    if (pState.ValueRO.Health <= 0) continue;

                    float3 pPos = pTransform.ValueRO.Position;
                    float dx = pPos.x - kothPos.x;
                    float dz = pPos.z - kothPos.z;
                    float distSq = (dx * dx) + (dz * dz);

                    if (distSq <= radiusSq && math.abs(pPos.y - kothPos.y) <= halfHeight)
                    {
                        if (pState.ValueRO.TeamIndex == 0) countA++;
                        else if (pState.ValueRO.TeamIndex == 1) countB++;
                    }
                }

                bool isContested = countA > 0 && countB > 0;
                koth.ValueRW.IsContested = isContested;

                int soleTeamOnPoint = -1;
                int cappingPlayers = 0;
                
                if (countA > 0 && countB == 0) { soleTeamOnPoint = 0; cappingPlayers = countA; }
                else if (countB > 0 && countA == 0) { soleTeamOnPoint = 1; cappingPlayers = countB; }
                
                koth.ValueRW.CappingPlayerCount = cappingPlayers;

                if (!isContested && soleTeamOnPoint != -1)
                {
                    if (soleTeamOnPoint == koth.ValueRO.CurrentOwner)
                    {
                        if (koth.ValueRO.CaptureProgress > 0)
                            koth.ValueRW.CaptureProgress = math.max(0f, koth.ValueRO.CaptureProgress - (dt * cappingPlayers));
                    }
                    else
                    {
                        if (koth.ValueRO.CapturingTeam != soleTeamOnPoint && koth.ValueRO.CaptureProgress > 0)
                        {
                            koth.ValueRW.CaptureProgress = math.max(0f, koth.ValueRO.CaptureProgress - (dt * cappingPlayers));
                        }
                        else
                        {
                            koth.ValueRW.CapturingTeam = soleTeamOnPoint;
                            koth.ValueRW.CaptureProgress += (dt * cappingPlayers);

                            if (koth.ValueRO.CaptureProgress >= koth.ValueRO.TimeToCapture)
                            {
                                koth.ValueRW.CurrentOwner = soleTeamOnPoint;
                                koth.ValueRW.CaptureProgress = 0f;
                            }
                        }
                    }
                }
                else if (!isContested)
                {
                    if (koth.ValueRO.CaptureProgress > 0)
                        koth.ValueRW.CaptureProgress = math.max(0f, koth.ValueRO.CaptureProgress - (dt * 0.5f));
                }

                if (koth.ValueRO.CurrentOwner == 0 && koth.ValueRO.TeamATimer > 0)
                    koth.ValueRW.TeamATimer = math.max(0f, koth.ValueRO.TeamATimer - dt);
                else if (koth.ValueRO.CurrentOwner == 1 && koth.ValueRO.TeamBTimer > 0)
                    koth.ValueRW.TeamBTimer = math.max(0f, koth.ValueRO.TeamBTimer - dt);

                bool aTimerZero = koth.ValueRO.TeamATimer <= 0;
                bool bTimerZero = koth.ValueRO.TeamBTimer <= 0;
                bool aOwns = koth.ValueRO.CurrentOwner == 0;
                bool bOwns = koth.ValueRO.CurrentOwner == 1;

                bool bContesting = countB > 0 || (koth.ValueRO.CapturingTeam == 1 && koth.ValueRO.CaptureProgress > 0);
                bool aContesting = countA > 0 || (koth.ValueRO.CapturingTeam == 0 && koth.ValueRO.CaptureProgress > 0);

                bool aWins = aTimerZero && aOwns && !bContesting;
                bool bWins = bTimerZero && bOwns && !aContesting;

                koth.ValueRW.IsOvertime = (aTimerZero && !aWins) || (bTimerZero && !bWins);

                // --- TRIGGER WIN ---
                if (aWins && !bWins) TriggerWin( koth, 0);
                else if (bWins && !aWins) TriggerWin( koth, 1);
                else if (aWins && bWins) TriggerWin( koth, koth.ValueRO.CurrentOwner);
            }
        }

        [BurstCompile]
        private void TriggerWin( in RefRW<KothPointComponent> koth, in int winningTeam)
        {
            koth.ValueRW.IsGameOver = true;
            koth.ValueRW.WinningTeam = winningTeam;
            koth.ValueRW.RestartTimer = 5f;
            koth.ValueRW.IsOvertime = false;
        }
    }
}