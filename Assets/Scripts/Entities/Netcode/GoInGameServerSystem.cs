using Entities.Netcode.GameMechanics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Entities.Netcode
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    internal partial struct PlayerServerJoinSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntitiesReferences>();
            state.RequireForUpdate<NetworkId>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var buffer = new EntityCommandBuffer(Allocator.Temp);
            var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

            // 1. Count players on each team
            int teamACount = 0;
            int teamBCount = 0;
            foreach (var pState in SystemAPI.Query<RefRO<PlayerStateComponent>>())
            {
                if (pState.ValueRO.TeamIndex == 0) teamACount++;
                else teamBCount++;
            }

            // Gather all spawn points into a temporary list
            var spawnPoints = new NativeList<LocalTransform>(Allocator.Temp);
            var spawnTeams = new NativeList<int>(Allocator.Temp);

            foreach (var (sp, transform) in SystemAPI.Query<RefRO<SpawnPointComponent>, RefRO<LocalTransform>>())
            {
                spawnPoints.Add(transform.ValueRO);
                spawnTeams.Add(sp.ValueRO.TeamIndex);
            }

            foreach (var (receiveRpcCommandRequest, entity) in SystemAPI
                         .Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRpc>().WithEntityAccess())
            {
                buffer.AddComponent<NetworkStreamInGame>(receiveRpcCommandRequest.ValueRO.SourceConnection);
                
                // 2. Assign team based on lowest count
                int assignedTeam = (teamACount <= teamBCount) ? 0 : 1;
                if (assignedTeam == 0) teamACount++;
                else teamBCount++;

                // 3. Find a valid spawn point for this team
                float3 spawnPos = float3.zero;
                var validSpawns = new NativeList<float3>(Allocator.Temp);
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    if (spawnTeams[i] == assignedTeam)
                        validSpawns.Add(spawnPoints[i].Position);
                }

                if (validSpawns.Length > 0)
                {
                    spawnPos = validSpawns[Random.Range(0, validSpawns.Length)];
                }
                else
                {
                    Debug.LogWarning($"[Server] No spawn points found for Team {assignedTeam}! Spawning at 0,0,0.");
                }

                validSpawns.Dispose();

                var playerEntity = buffer.Instantiate(entitiesReferences.PlayerPrefabEntity);
                buffer.SetComponent(playerEntity, LocalTransform.FromPosition(spawnPos));
                var networkId = SystemAPI.GetComponent<NetworkId>(receiveRpcCommandRequest.ValueRO.SourceConnection);
                buffer.SetComponent(playerEntity, new PlayerStateComponent
                {
                    Health = 100f,
                    TeamIndex = assignedTeam,
                    NetworkId = networkId.Value
                });

                buffer.AddComponent(playerEntity, new GhostOwner { NetworkId = networkId.Value });
                buffer.AppendToBuffer(receiveRpcCommandRequest.ValueRO.SourceConnection, new LinkedEntityGroup { Value = playerEntity });

                Debug.Log($"[Server] Player connected. Assigned to Team {assignedTeam}.");

                buffer.DestroyEntity(entity);
            }

            spawnPoints.Dispose();
            spawnTeams.Dispose();
            buffer.Playback(state.EntityManager);
        }
    }
}