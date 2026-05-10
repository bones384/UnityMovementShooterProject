using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Entities.Netcode
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    partial class PlayerInputSystem : SystemBase
    {
        private InputSystem_Actions _controls;        
        protected override void OnCreate()
        {
            _controls = new();
            _controls.Enable();
            RequireForUpdate<NetworkStreamInGame>();
            RequireForUpdate<PlayerInput>();
        }

        protected override void OnUpdate()
        {
            foreach (var playerInput in SystemAPI.Query<RefRW<PlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                float2 inputVector = _controls.Player.Move.ReadValue<Vector2>();
                playerInput.ValueRW.inputVector = inputVector;
            }
        }
        
    }
}