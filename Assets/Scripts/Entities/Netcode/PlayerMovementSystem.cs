using System.Numerics;
using Entities.Netcode;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
            maxSpeed = entitiesReferences.MaxSpeed;
            acceleration = entitiesReferences.Acceleration;
            jumpSpeed = entitiesReferences.JumpSpeed;
            
            foreach ((var playerInput, var localTransform) in SystemAPI
                         .Query<RefRO<Entities.Netcode.PlayerInput>, RefRW<LocalTransform>>().WithAll<Simulate>())
            {
                float moveSpeed = 10f;
                float3 moveVector = new(playerInput.ValueRO.InputMovementVector.x, 0, playerInput.ValueRO.InputMovementVector.y);
                var lookVector = playerInput.ValueRO.InputLookVector;
                localTransform.ValueRW.Position += moveVector * moveSpeed * SystemAPI.Time.DeltaTime;
            }
        }


    }
}