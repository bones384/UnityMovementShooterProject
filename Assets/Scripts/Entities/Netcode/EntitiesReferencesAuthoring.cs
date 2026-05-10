using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode
{
    public class EntitiesReferenceAuthoring : MonoBehaviour
    {
        
        public GameObject playerPrefab;
        public class EntitiesReferenceBaker : Baker<EntitiesReferenceAuthoring>
        {
            public override void Bake(EntitiesReferenceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EntitiesReferences
                {
                    playerPrefabEntity = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    public struct EntitiesReferences : IComponentData
    {
        public Entity playerPrefabEntity;
    }
}