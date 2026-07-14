using Unity.Entities;

namespace Entities.Netcode
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class AudioPlaybackSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (AudioManager.Instance == null || PlayerVisualisationManager.LocalPlayer == null) return;

            int myNetworkId = PlayerVisualisationManager.LocalPlayer.Value.NetworkId;

            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (request, entity) in SystemAPI.Query<RefRO<AudioRequest>>().WithEntityAccess())
            {
                bool isForEveryone = request.ValueRO.LocalTargetNetworkId == -1;
                bool isForMe = request.ValueRO.LocalTargetNetworkId == myNetworkId;

                if (isForEveryone || isForMe)
                {
                    if (request.ValueRO.Effect == SFX.HitConfirm || request.ValueRO.Effect == SFX.TakeDamage)
                        AudioManager.Instance.Play2D(request.ValueRO.Effect, request.ValueRO.Pitch);
                    else
                        AudioManager.Instance.Play3D(request.ValueRO.Effect, request.ValueRO.Position, request.ValueRO.Pitch);
                }

                // Destroy immediately so it only plays once
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}