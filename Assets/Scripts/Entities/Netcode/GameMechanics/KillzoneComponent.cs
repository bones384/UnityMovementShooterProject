using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode.GameMechanics
{
    // --- 1. AUTHORING & BAKER ---
    public class KillzoneAuthoring : MonoBehaviour
    {
        // You can add properties here later (e.g., specific damage types), 
        // but an empty MonoBehaviour is all we need to tag the Entity!
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