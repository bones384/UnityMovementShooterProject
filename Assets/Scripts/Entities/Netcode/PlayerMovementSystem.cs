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
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Entities.Netcode.PlayerInput>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((var playerInput, var localTransform) in SystemAPI
                         .Query<RefRO<Entities.Netcode.PlayerInput>, RefRW<LocalTransform>>().WithAll<Simulate>())
            {
                float moveSpeed = 10f;
                float3 moveVector = new(playerInput.ValueRO.inputVector.x, 0, playerInput.ValueRO.inputVector.y);
                localTransform.ValueRW.Position += moveVector * moveSpeed * SystemAPI.Time.DeltaTime;
            }
        }


    }
}