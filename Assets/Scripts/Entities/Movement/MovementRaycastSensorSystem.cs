using Entities.Netcode;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Entities.Movement
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(PlayerMovementSystem))]
    //[UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct MovementRaycastSensorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<MovementRaycasterComponent>();
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var groundDistance = 1.1f;
            var wallDistance = 0.5f;
            foreach (var (transform, contacts, simulate, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRW<MovementRaycasterComponent>, RefRO<Simulate>>()
                         .WithEntityAccess())
            {
                var pos = transform.ValueRO.Position + new float3(0, 1, 0);
                Debug.DrawLine(pos, pos + math.down() * groundDistance, Color.red);
                contacts.ValueRW.IsGrounded =
                    Raycast(collisionWorld, pos, math.down(), groundDistance, out var hit);
                if (contacts.ValueRW.IsGrounded)
                {
                    if (math.degrees(math.acos(math.dot(math.normalize(new float3(0, 1, 0)),
                            math.normalize(hit.SurfaceNormal)))) >= 45)
                        contacts.ValueRW.IsGrounded = false;

                    contacts.ValueRW.GroundNormal = hit.SurfaceNormal;
                    contacts.ValueRW.GroundHit = hit.Position;
                }


                Debug.DrawLine(pos, pos - transform.ValueRO.Right() * wallDistance, Color.blue);
                Debug.DrawLine(pos, pos + transform.ValueRO.Right() * wallDistance, Color.green);


                contacts.ValueRW.HasWallLeft =
                    Raycast(collisionWorld, pos, -transform.ValueRO.Right(), wallDistance, out hit);
                if (contacts.ValueRW.HasWallLeft)
                {
                    contacts.ValueRW.WallLeftNormal = hit.SurfaceNormal;
                    contacts.ValueRW.WallLeftHit = hit.Position;
                }

                contacts.ValueRW.HasWallRight =
                    Raycast(collisionWorld, pos, transform.ValueRO.Right(), wallDistance, out hit);
                {
                    if (contacts.ValueRW.HasWallRight) contacts.ValueRW.WallRightNormal = hit.SurfaceNormal;
                    contacts.ValueRW.WallRightHit = hit.Position;
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }


        private static bool Raycast(
            CollisionWorld collisionWorld,
            float3 origin,
            float3 direction,
            float distance, out RaycastHit hit)
        {
            var input = new RaycastInput
            {
                Start = origin,
                End = origin + direction * distance,
                Filter =
                {
                    BelongsTo = 1u << 7, // Raycast as player
                    CollidesWith = 1u << 6, // Raycast against level
                    GroupIndex = 0
                }
            };

            return collisionWorld.CastRay(input, out hit);
        }
    }
}