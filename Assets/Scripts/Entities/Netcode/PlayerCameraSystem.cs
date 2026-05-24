using Entities.Netcode;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct PlayerBridgingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LocalPlayerTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var transform
                 in SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<LocalPlayerTag>())
        {
            PlayerBridge.Instance.PlayerState = new PlayerState()
            {
                Position = transform.ValueRO.Position,
                Rotation = transform.ValueRO.Rotation,
                IsCrouching = false,
                IsJumping = false,
                IsParrying = false,
                IsSliding = false,
                IsWallRunning = false,
                Velocity = Vector3.zero
            };

        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
