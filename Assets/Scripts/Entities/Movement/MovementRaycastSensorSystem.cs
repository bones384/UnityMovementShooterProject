using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Entities.Movement
{
    
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
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
            float groundDistance = 1.1f;
            float wallDistance = 0.5f;
            foreach (var (transform, contacts, simulate, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRW<MovementRaycasterComponent>, RefRO<Simulate>>()
                         
                         .WithEntityAccess())
            {
                float3 pos = transform.ValueRO.Position + new float3(0, 1, 0);
                Debug.DrawLine(pos, pos + math.down() * groundDistance, Color.red);
                contacts.ValueRW.IsGrounded =
                    Raycast( collisionWorld, pos, math.down(), groundDistance, out var hit);
                if( contacts.ValueRW.IsGrounded) contacts.ValueRW.GroundNormal = hit.SurfaceNormal;
                 Debug.DrawLine(pos, pos - transform.ValueRO.Right() * wallDistance, Color.blue);
                 Debug.DrawLine(pos, pos + transform.ValueRO.Right() * wallDistance, Color.green);
                
                
                    contacts.ValueRW.HasWallLeft =
                           Raycast(collisionWorld, pos, -transform.ValueRO.Right(), wallDistance, out hit);
        if(contacts.ValueRW.HasWallLeft) contacts.ValueRW.WallLeftNormal = hit.SurfaceNormal;
                         contacts.ValueRW.HasWallRight =
                    Raycast(collisionWorld, pos, transform.ValueRO.Right(), wallDistance, out hit);  
                         if(contacts.ValueRW.HasWallRight) contacts.ValueRW.WallRightNormal = hit.SurfaceNormal;
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
            float distance, out Unity.Physics.RaycastHit hit)
        {
            var input = new RaycastInput
            {
                Start = origin,
                End = origin + direction * distance,
                Filter =
                {
                    BelongsTo = 1u << 7, // Raycast against everything
                    CollidesWith = 1u << 6, // Raycast against everything
                    GroupIndex = 0,
                }
            };

            return collisionWorld.CastRay(input, out hit);
        }
        
    }
}