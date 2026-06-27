using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

public struct LocalPlayerTag : IComponentData
{
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
internal partial struct FindPlayerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (owner, entity)
                 in SystemAPI.Query<RefRO<GhostOwner>>().WithAll<Player>()
                     .WithEntityAccess())
            if (owner.ValueRO.NetworkId ==
                SystemAPI.GetSingleton<NetworkId>().Value)
                if (!state.EntityManager.HasComponent<LocalPlayerTag>(entity))
                    entityCommandBuffer.AddComponent<LocalPlayerTag>(entity);

        entityCommandBuffer.Playback(state.EntityManager);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}