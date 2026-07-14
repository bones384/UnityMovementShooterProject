using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Entities.Netcode
{
    public struct PlayerViewTag : IComponentData
    {
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial class PlayerVisualisationSystem : SystemBase
    {
        // public GameObject PlayerViewPrefab;

        protected override void OnCreate()
        {
            RequireForUpdate<Player>();
        }

        protected override void OnUpdate()
        {
            if (World.IsServer())
                return;
            var localNetworkId =
                SystemAPI.GetSingleton<NetworkId>().Value;

            EntityCommandBuffer buffer = new(Allocator.Temp);
            foreach (var (transform, ghostOwner, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<GhostOwner>>().WithAll<Player>()
                         .WithNone<PlayerViewTag>()
                         .WithEntityAccess())
            {
                var view = PlayerVisualisationManager.CreatePlayerView();
                view.isLocalPlayer = ghostOwner.ValueRO.NetworkId == localNetworkId;
                PlayerVisualisationManager.PlayerViewRegistry.Views[entity] = view;
                buffer.AddComponent<PlayerViewTag>(entity);
            }

            buffer.Playback(EntityManager);

            foreach (var (playerstate, entity)
                     in SystemAPI.Query<RefRO<PlayerStateComponent>>().WithAll<Player>()
                         .WithAll<PlayerViewTag>()
                         .WithEntityAccess())
            {
                var view = PlayerVisualisationManager.PlayerViewRegistry.Views[entity];
                view.PlayerState = playerstate.ValueRO;

                if(!playerstate.ValueRO.IsDead)view.transform.position = playerstate.ValueRO.Position;
                view.transform.rotation = playerstate.ValueRO.Rotation;
            }
        }
    }
}