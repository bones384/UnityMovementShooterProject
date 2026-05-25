using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Entities.Movement
{
    public struct MovementRaycasterComponent : IComponentData
    {
        public bool IsGrounded;
        public bool HasWallLeft;
        public bool HasWallRight;

        public float3 GroundNormal;
        public float3 WallLeftNormal;
        public float3 WallRightNormal;
    }

    public class MovementRaycasterComponentAuthoring : MonoBehaviour
    {
        public class MovementRaycasterComponentBaker : Baker<MovementRaycasterComponentAuthoring>
        {
            public override void Bake(MovementRaycasterComponentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<MovementRaycasterComponent>(entity);
            }
        }
    }
}