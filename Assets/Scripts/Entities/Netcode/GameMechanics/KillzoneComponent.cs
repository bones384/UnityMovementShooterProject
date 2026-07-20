using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode.GameMechanics
{
    public class KillzoneAuthoring : MonoBehaviour
    {

    }

    public class KillzoneBaker : Baker<KillzoneAuthoring>
    {
        public override void Bake(KillzoneAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new KillzoneTag());
        }
    }

    public struct KillzoneTag : IComponentData
    {
    }
}