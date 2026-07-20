using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Entities.Netcode.Shooting
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(ProjectileSimulationSystem))]
    public partial struct ProjectileSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntitiesReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var prefab = SystemAPI.GetSingleton<EntitiesReferences>().ProjectilePrefab;

            foreach (var (request, entity) in SystemAPI.Query<RefRO<ProjectileSpawnRequest>>().WithEntityAccess())
            {
                var projectileEntity = ecb.Instantiate(prefab);

                ecb.SetComponent(projectileEntity, LocalTransform.FromPositionRotation(
                    request.ValueRO.Origin,
                    quaternion.LookRotationSafe(request.ValueRO.Direction, math.up())
                ));
                
                ecb.SetComponent(projectileEntity, new ProjectileComponent
                {
                    Owner = request.ValueRO.Owner,
                    Velocity = request.ValueRO.Direction * request.ValueRO.Speed,
                    Damage = request.ValueRO.Damage,
                    Lifespan = request.ValueRO.Lifespan,
                    ApplyGravity = request.ValueRO.ApplyGravity,
                    Radius = request.ValueRO.Radius
                });

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}