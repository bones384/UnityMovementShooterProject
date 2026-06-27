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
            // state.RequireForUpdate<EnableCharacterController>();
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<LocalPlayerTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var camera = Camera.main;
            if (camera == null) return;
            //We need to access the LocalToWorld matrix to match the position of the player in term of presentation.
            //Because Physics can be either Interpolated or Predicted, we the LocalToWorld can be different than the real world position
            //of the entity.
            foreach (var (localToWorld, input) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<PlayerLook>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                camera.transform.rotation = math.mul(quaternion.RotateY(input.ValueRO.Yaw),
                    quaternion.RotateX(-input.ValueRO.Pitch));
                //var offset = math.rotate(camera.transform.rotation, input.ValueRO.CameraOffset);
                camera.transform.position = localToWorld.ValueRO.Position + input.ValueRO.CameraOffset;
            }
        }
    }
}