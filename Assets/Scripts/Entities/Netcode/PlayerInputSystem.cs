using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Entities.Netcode
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    internal partial class PlayerInputSystem : SystemBase
    {
        private InputSystem_Actions _controls;

        protected override void OnCreate()
        {
            _controls = new InputSystem_Actions();
            _controls.Enable();
            RequireForUpdate<NetworkStreamInGame>();
            RequireForUpdate<PlayerInput>();
        }

        protected override void OnUpdate()
        {
            foreach (var playerInput in SystemAPI.Query<RefRW<PlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                float2 inputVector = _controls.Player.Move.ReadValue<Vector2>();
                float2 lookvec = _controls.Player.Look.ReadValue<Vector2>();
                playerInput.ValueRW.InputMovementVector = inputVector;
                playerInput.ValueRW.InputLookVector = lookvec;
                playerInput.ValueRW.JumpInput = _controls.Player.Jump.triggered;
                playerInput.ValueRW.CrouchInput = _controls.Player.Crouch.triggered;
                playerInput.ValueRW.ParryInput = _controls.Player.Parry.triggered;
                playerInput.ValueRW.SprintInput = _controls.Player.Sprint.triggered;
                playerInput.ValueRW.PrimaryAbilityInput = _controls.Player.Primary.triggered;
                playerInput.ValueRW.SecondaryAbilityInput = _controls.Player.Secondary.triggered;
                playerInput.ValueRW.FourthAbilityInput = _controls.Player.Ability4.triggered;
                playerInput.ValueRW.ThirdAbilityInput = _controls.Player.Ability3.triggered;
            }
        }
    }
}