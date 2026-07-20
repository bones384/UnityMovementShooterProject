using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Entities.Netcode
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    internal partial struct CharacterControllerCameraSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<LocalPlayerTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var camera = Camera.main;
            if (camera == null) return;

            foreach (var (localToWorld, input, pState) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<PlayerLook>, RefRO<PlayerStateComponent>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                camera.transform.rotation = math.mul(quaternion.RotateY(input.ValueRO.Yaw),
                    quaternion.RotateX(-input.ValueRO.Pitch));

                if (!pState.ValueRO.IsDead)
                    camera.transform.position = localToWorld.ValueRO.Position + input.ValueRO.CameraOffset;
                else
                    camera.transform.position = pState.ValueRO.DeathPosition + input.ValueRO.CameraOffset;
            }
        }
    }
}