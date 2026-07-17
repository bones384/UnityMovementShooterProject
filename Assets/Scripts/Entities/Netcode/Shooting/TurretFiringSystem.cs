using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Entities.Netcode.Shooting
{
    // Server-Only, and perfectly safe to run in the standard Simulation group
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct TurretSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // We only need NetworkTime to ensure Netcode is actively running
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Because this is Server-Only, DeltaTime is 100% safe from rollbacks!
            var dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (turret, transform, entity) in SystemAPI
                         .Query<RefRW<TurretComponent>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (!turret.ValueRO.IsInitialized)
                {
                    turret.ValueRW.IsInitialized = true;
                    turret.ValueRW.Timer = turret.ValueRO.FireInterval;
                    continue;
                }

                turret.ValueRW.Timer -= dt;

                // A while loop instead of an IF statement guarantees mathematical 
                // consistency, preventing bullets from clumping if the server lags.
                while (turret.ValueRO.Timer <= 0f)
                {
                    turret.ValueRW.Timer += turret.ValueRO.FireInterval;


                    var reqEntity = ecb.CreateEntity();
                    var spawnOrigin = transform.ValueRO.Position + transform.ValueRO.Forward() * 1.5f;

                    ecb.AddComponent(reqEntity, new ProjectileSpawnRequest
                    {
                        Owner = entity,
                        Origin = spawnOrigin,
                        Direction = transform.ValueRO.Forward(),
                        Speed = turret.ValueRO.ProjectileSpeed,
                        Damage = turret.ValueRO.ProjectileDamage,
                        Lifespan = turret.ValueRO.ProjectileLifespan,
                        ApplyGravity = turret.ValueRO.ProjectileApplyGravity,
                        Radius = turret.ValueRO.ProjectileRadius
                    });
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}