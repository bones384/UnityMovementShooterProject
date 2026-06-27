using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Entities.Netcode
{
    public class PlayerStateComponentAuthoring : MonoBehaviour
    {
    }

    public class PlayerStateComponentAuthoringBaker : Baker<PlayerStateComponentAuthoring>
    {
        public override void Bake(PlayerStateComponentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerStateComponent());
        }
    }

    public struct PlayerStateComponent : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public float3 Velocity;
        public bool IsSliding;
        public bool IsWallRunning;
        public bool IsCrouching;
        public bool IsJumping;
        public bool IsParrying;
        public bool IsGrounded;
    }
}