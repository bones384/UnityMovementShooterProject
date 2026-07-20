using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
internal partial struct PlayerBridgingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LocalPlayerTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var transform
                 in SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<LocalPlayerTag>())
        {

        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}