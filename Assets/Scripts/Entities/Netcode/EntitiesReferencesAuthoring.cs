using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode
{
    public class EntitiesReferenceAuthoring : MonoBehaviour
    {
        
        public GameObject playerPrefab;

        public float maxSpeed = 20;
        public float acceleration = 5.0f;
        public float jumpSpeed = 8.0f;
    }

    class RotationSpeedBaker : Baker<EntitiesReferenceAuthoring>
    {
        public override void Bake(EntitiesReferenceAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntitiesReferences
            {
                PlayerPrefabEntity = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic),
                MaxSpeed = authoring.maxSpeed,
                Acceleration = authoring.acceleration,
                JumpSpeed = authoring.jumpSpeed
            });
        }
    }

    public struct EntitiesReferences : IComponentData
    {
        public Entity PlayerPrefabEntity;
        public float MaxSpeed;
        public float Acceleration;
        public float JumpSpeed;
    }
}