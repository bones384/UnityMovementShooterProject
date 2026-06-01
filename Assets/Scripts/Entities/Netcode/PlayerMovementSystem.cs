using System.Numerics;
using Entities.Netcode;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using Unity.VersionControl.Git;
using UnityEngine;

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
            state.RequireForUpdate<PhysicsWorldSingleton>();
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

            var time = SystemAPI.GetSingleton<NetworkTime>();

            foreach ((var playerInput, var localTransform, var playerLook, var collider) in SystemAPI
                         .Query<RefRO<Entities.Netcode.PlayerInput>, RefRW<LocalTransform>, RefRW<PlayerLook>, RefRO<PhysicsCollider>>().WithAll<Simulate>())
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
           
                var displacement = move * moveSpeed * SystemAPI.Time.DeltaTime;
                
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                
                
            
                
                var Collider = collider.ValueRO.Value;
               var collision = SCast(localTransform.ValueRO.Position, localTransform.ValueRO.Position + displacement, Collider);

               if(collision==Entity.Null) localTransform.ValueRW.Position += displacement;



            }
            foreach ((var playerInput, var localTransform, var playerLook, var playerState) in SystemAPI
                         .Query<RefRO<Entities.Netcode.PlayerInput>, RefRO<LocalTransform>, RefRO<PlayerLook>, RefRW<PlayerStateComponent>>())
            {
                playerState.ValueRW.Position = localTransform.ValueRO.Position;
                playerState.ValueRW.Rotation = localTransform.ValueRO.Rotation;
                playerState.ValueRW.IsJumping = playerInput.ValueRO.JumpInput;
            }
        }
        
        public unsafe Entity SCast(float3 RayFrom, float3 RayTo, BlobAssetReference<Unity.Physics.Collider> radius)
        {            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            
            var filter = new CollisionFilter()
            {
                BelongsTo = 1u << 7, // Raycast against everything
                CollidesWith = 1u << 6, // Raycast against everything
                GroupIndex = 0,
            };
            fixed (Unity.Physics.Collider* rad = &radius.Value)
            {
                rad->SetCollisionFilter(filter);



                var input = new ColliderCastInput()
                {
                    Start = RayFrom,
                    End = RayTo,
                    Collider = rad,
                    Orientation = quaternion.identity
                };
                ColliderCastHit hit = new ColliderCastHit();

                bool haveHit = collisionWorld.CastCollider(input, out hit);
                if (haveHit)
                {
                    return hit.Entity;
                }
            }
 

            return Entity.Null;
        }


    }
}