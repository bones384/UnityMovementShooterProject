using Entities.Netcode.Shooting;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Entities.Netcode.GameMechanics
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileSimulationSystem))]
    public partial struct PlayerDeathRespawnSystem : ISystem
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

            var spawnPoints = new NativeList<LocalTransform>(Allocator.Temp);
            var spawnTeams = new NativeList<int>(Allocator.Temp);

            foreach (var (sp, transform) in SystemAPI.Query<RefRO<SpawnPointComponent>, RefRO<LocalTransform>>())
            {
                spawnPoints.Add(transform.ValueRO);
                spawnTeams.Add(sp.ValueRO.TeamIndex);
            }

            foreach (var (pState, transform, entity) in SystemAPI
                         .Query<RefRW<PlayerStateComponent>, RefRW<LocalTransform>>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
                if (pState.ValueRO.Health <= 0f)
                {
                    if (!pState.ValueRO.IsDead)
                    {
                        if (networkTime.IsFirstTimeFullyPredictingTick)
                        {
                            var audioReq = ecb.CreateEntity();
                            ecb.AddComponent(audioReq,
                                new AudioRequest
                                {
                                    Effect = SFX.Death, Position = transform.ValueRO.Position, Pitch = 1f,
                                    LocalTargetNetworkId = -1
                                });
                        }

                        pState.ValueRW.IsDead = true;
                        pState.ValueRW.DeathCount++;
                        pState.ValueRW.RespawnTimer = entitiesReferences.RespawnDelay;
                        pState.ValueRW.DeathPosition = transform.ValueRO.Position;
                        transform.ValueRW.Position = new float3(0, -1000, 0);
                    }
                    
                    if (pState.ValueRO.RespawnTimer > 0) pState.ValueRW.RespawnTimer -= dt;
                    
                    if (pState.ValueRO.RespawnTimer <= 0)
                    {
                        pState.ValueRW.IsDead = false;
                        pState.ValueRW.Health = 100f;
                        pState.ValueRW.Velocity = float3.zero;
                        pState.ValueRW.IsParrying = false;
                        pState.ValueRW.IsWallRunning = false;
                        pState.ValueRW.CurrentAmmo = entitiesReferences.MagazineSize;
                        var validSpawns = new NativeList<float3>(Allocator.Temp);
                        for (var i = 0; i < spawnPoints.Length; i++)
                            if (spawnTeams[i] == pState.ValueRO.TeamIndex)
                                validSpawns.Add(spawnPoints[i].Position);

                        if (validSpawns.Length > 0)
                        {
                            var seed = (networkTime.ServerTick.TickIndexForValidTick ^ (uint)entity.Index) + 997u;
                            var rand = new Random(seed);
                            var randomIndex = rand.NextInt(0, validSpawns.Length);
                            transform.ValueRW.Position = validSpawns[randomIndex];
                        }
                        else
                        {
                            transform.ValueRW.Position = float3.zero;
                        }

                        validSpawns.Dispose();
                    }
                }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            spawnPoints.Dispose();
            spawnTeams.Dispose();
        }
    }
}