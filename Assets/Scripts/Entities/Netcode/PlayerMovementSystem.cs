using System.Numerics;
using Entities.Netcode;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.VersionControl.Git;

namespace Entities.Netcode{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    partial struct PlayerMovementSystem :
    ISystem
    {

        public float maxSpeed;
        public float acceleration;
        public float jumpSpeed; 
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntitiesReferences>();
            state.RequireForUpdate<Entities.Netcode.PlayerInput>();
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
            maxSpeed = entitiesReferences.MaxSpeed;
            acceleration = entitiesReferences.Acceleration;
            jumpSpeed = entitiesReferences.JumpSpeed;
            
            foreach ((var playerInput, var localTransform, var playerLook) in SystemAPI
                         .Query<RefRO<Entities.Netcode.PlayerInput>, RefRW<LocalTransform>, RefRW<PlayerLook>>().WithAll<Simulate>())
            {
                var lookVector = playerInput.ValueRO.InputLookVector;
                
                const float userSpecifiedMouseSensitivity = 1f;
                
                lookVector *= userSpecifiedMouseSensitivity * SystemAPI.Time.DeltaTime;
                
                
                var cameraRotation = math.mul(quaternion.RotateY(playerLook.ValueRW.Yaw), quaternion.RotateX(0));
                localTransform.ValueRW.Rotation = cameraRotation;
                var forward = math.mul(cameraRotation, math.forward());
                
                playerLook.ValueRW.Pitch = math.clamp(playerLook.ValueRW.Pitch+lookVector.y, -math.PI/2, math.PI/2);
                playerLook.ValueRW.Yaw = math.fmod(playerLook.ValueRW.Yaw + lookVector.x, 2*math.PI);
                
                float moveSpeed = 10f;
                float3 move =
                    localTransform.ValueRO.Right() * playerInput.ValueRO.InputMovementVector.x +
                    localTransform.ValueRO.Forward() * playerInput.ValueRO.InputMovementVector.y;
                
                move =  math.normalizesafe(move);
           
                localTransform.ValueRW.Position += move * moveSpeed * SystemAPI.Time.DeltaTime;;
                
             
                
            }
            foreach ((var playerInput, var localTransform, var playerLook, var playerState) in SystemAPI
                         .Query<RefRO<Entities.Netcode.PlayerInput>, RefRO<LocalTransform>, RefRO<PlayerLook>, RefRW<PlayerStateComponent>>())
            {
                playerState.ValueRW.Position = localTransform.ValueRO.Position;
                playerState.ValueRW.Rotation = localTransform.ValueRO.Rotation;
                playerState.ValueRW.IsJumping = playerInput.ValueRO.JumpInput;
            }
        }


    }
}