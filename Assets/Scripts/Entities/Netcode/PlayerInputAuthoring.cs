using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

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
        public float2 InputMovementVector;
        public float2 InputLookVector;
        
        public bool SprintInput;
        
        public bool JumpInput;
        public bool CrouchInput;
        public bool ParryInput;
        
        public bool PrimaryAbilityInput;
        public bool SecondaryAbilityInput;
        
        public bool ThirdAbilityInput;
        public bool FourthAbilityInput;
    }
}