using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode
{
    public class EntitiesReferenceAuthoring : MonoBehaviour
    {
        public GameObject playerPrefab;

        public float maxSpeed = 20;
        public float initialSpeed = 16;
        public float gravity = -9.81f;
        public float acceleration = 4f;
        public float jumpSpeed = 8.0f;
        public float dampenSpeed = 0.6f;
        public float maxFallSpeed = 10;
    }

    internal class RotationSpeedBaker : Baker<EntitiesReferenceAuthoring>
    {
        public override void Bake(EntitiesReferenceAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntitiesReferences
            {
                PlayerPrefabEntity = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic),
                MaxSpeed = authoring.maxSpeed,
                Acceleration = authoring.acceleration,
                JumpSpeed = authoring.jumpSpeed,
                InitialSpeed = authoring.initialSpeed,
                Gravity = authoring.gravity,
                DampenSpeed = authoring.dampenSpeed,
                MaxFallSpeed = authoring.maxFallSpeed
            });
        }
    }

    public struct EntitiesReferences : IComponentData
    {
        public Entity PlayerPrefabEntity;
        public float MaxSpeed;
        public float Acceleration;
        public float JumpSpeed;
        public float InitialSpeed;
        public float Gravity;
        public float DampenSpeed;
        public float MaxFallSpeed;
    }
}