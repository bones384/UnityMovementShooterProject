using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Entities.Netcode
{
    class PlayerInputAuthoring : MonoBehaviour
    {

    }

    class PlayerInputAuthoringBaker : Baker<PlayerInputAuthoring>
    {
        public override void Bake(PlayerInputAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerInput());
        }
    }

    public struct PlayerInput : IInputComponentData
    {
        public float2 inputVector;
    }
}