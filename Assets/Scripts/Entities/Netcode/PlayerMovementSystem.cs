using System;
using Entities.Movement;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Entities.Netcode
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    internal partial struct PlayerMovementSystem : ISystem
    {
        public float maxSpeed;
        public float acceleration;
        public float jumpSpeed;
        public float initialSpeed;
        public float gravity;
        public float dampenSpeed;
        public float maxFallSpeed;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<EntitiesReferences>();
            state.RequireForUpdate<PlayerInput>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Action stopSliding = () => { }; //WIP


            var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

            maxSpeed = entitiesReferences.MaxSpeed;
            acceleration = entitiesReferences.Acceleration;
            jumpSpeed = entitiesReferences.JumpSpeed;
            initialSpeed = entitiesReferences.InitialSpeed;
            gravity = entitiesReferences.Gravity;
            dampenSpeed = entitiesReferences.DampenSpeed;
            maxFallSpeed = entitiesReferences.MaxFallSpeed;

            foreach (var (playerInput, localTransform, playerLook, collider, pstate, contacts) in SystemAPI
                         .Query<RefRO<PlayerInput>, RefRW<LocalTransform>, RefRW<PlayerLook>,
                             RefRO<PhysicsCollider>, RefRW<PlayerStateComponent>, RefRW<MovementRaycasterComponent>>()
                         .WithAll<Simulate>())
            {
                var lookVector = playerInput.ValueRO.InputLookVector;

                const float userSpecifiedMouseSensitivity = 1f;
                lookVector *= userSpecifiedMouseSensitivity * SystemAPI.Time.DeltaTime;


                var cameraRotation = math.mul(quaternion.RotateY(playerLook.ValueRW.Yaw), quaternion.RotateX(0));
                localTransform.ValueRW.Rotation = cameraRotation;
                // var forward = math.mul(cameraRotation, math.forward());

                playerLook.ValueRW.Pitch =
                    math.clamp(playerLook.ValueRW.Pitch + lookVector.y, -math.PI / 2, math.PI / 2);
                playerLook.ValueRW.Yaw = math.fmod(playerLook.ValueRW.Yaw + lookVector.x, 2 * math.PI);


                //var moveSpeed = pstate.ValueRO.Velocity;
                //var moveSpeed = 4f;

                var move =
                    localTransform.ValueRO.Right() * playerInput.ValueRO.InputMovementVector.x +
                    localTransform.ValueRO.Forward() * playerInput.ValueRO.InputMovementVector.y;


                move = math.normalizesafe(move);
                var displacement = float3.zero;

                var isGrounded = contacts.ValueRO.IsGrounded;
                pstate.ValueRW.IsGrounded = isGrounded;

                if (!isGrounded)
                {
                    //apply gravity (but do not fall faster than maxSpeed)

                    pstate.ValueRW.Velocity.y = -pstate.ValueRO.Velocity.y < maxFallSpeed
                        ? pstate.ValueRW.Velocity.y + gravity * SystemAPI.Time.DeltaTime
                        : pstate.ValueRW.Velocity.y = -maxFallSpeed;
                }
                else
                {
                    localTransform.ValueRW.Position = contacts.ValueRO.GroundHit;
                    pstate.ValueRW.Velocity.y = 0;
                } //snap to floor


                if (move.Equals(float3.zero))
                    if (isGrounded)
                        pstate.ValueRW.Velocity *= dampenSpeed;


                //apply movement
                var horizontalSpeed = math.length(new float3(pstate.ValueRO.Velocity.x, 0, pstate.ValueRO.Velocity.z));
                var verticalVelocity = new float3(0, pstate.ValueRO.Velocity.y, 0);
                var speedToApply = math.max(maxSpeed, horizontalSpeed);

                if (speedToApply > maxSpeed) speedToApply *= 0.985f;

                var newVelocity = move * speedToApply;
                pstate.ValueRW.Velocity = new float3(newVelocity.x, pstate.ValueRO.Velocity.y, newVelocity.z);

                displacement += newVelocity * SystemAPI.Time.DeltaTime;
                displacement += verticalVelocity * SystemAPI.Time.DeltaTime;


                BlobAssetReference<Collider> capsuleCollider;
                var filter = new CollisionFilter
                {
                    BelongsTo = 1u << 7, // Is on player layer
                    CollidesWith = 1u << 6, // Raycast against level layer
                    GroupIndex = 0
                };


                var halfSegment = math.max(0f, 1.8f - 2f * 0.45f) * 0.5f;

                var capsuleGeometry = new CapsuleGeometry
                {
                    Radius = 0.45f,
                    Vertex0 = new float3(0, 1, 0) + new float3(0, -halfSegment, 0),
                    Vertex1 = new float3(0, 1, 0) + new float3(0, halfSegment, 0)
                };

                capsuleCollider = CapsuleCollider.Create(capsuleGeometry, filter);
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

                localTransform.ValueRW.Position = CollideAndSlide_Linahan(collisionWorld, capsuleCollider,
                    localTransform.ValueRO.Position, localTransform.ValueRO.Rotation,
                    newVelocity * SystemAPI.Time.DeltaTime, filter);

                localTransform.ValueRW.Position = CollideAndSlide_Linahan(collisionWorld, capsuleCollider,
                    localTransform.ValueRO.Position, localTransform.ValueRO.Rotation,
                    verticalVelocity * SystemAPI.Time.DeltaTime, filter);
                capsuleCollider.Dispose();
            }

            foreach (var (playerInput, localTransform, playerLook, playerState) in SystemAPI
                         .Query<RefRO<PlayerInput>, RefRO<LocalTransform>, RefRO<PlayerLook>,
                             RefRW<PlayerStateComponent>>())
            {
                playerState.ValueRW.Position = localTransform.ValueRO.Position;
                playerState.ValueRW.Rotation = localTransform.ValueRO.Rotation;
                playerState.ValueRW.IsJumping = playerInput.ValueRO.JumpInput;
            }
        }
        
        [BurstCompile]
        public static unsafe float3 CollideAndSlide_Linahan(
            CollisionWorld world,
            BlobAssetReference<Collider> capsuleCollider,
            float3 position,
            quaternion orientation,
            float3 velocity,
            CollisionFilter filter,
            int maxIterations = 3,
            float skinWidth = 0.015f)
        {
            var pos = position;
            var vel = velocity;
            var dest = pos + vel;

            // Stored constraint normals (sliding planes)
            var n1 = float3.zero;
            var n2 = float3.zero;
            var planeCount = 0;

            var colliderPtr = (Collider*)capsuleCollider.GetUnsafePtr();

            for (var i = 0; i < maxIterations; i++)
            {
                var remainingDist = math.length(vel);
                if (remainingDist <= skinWidth)
                    break;

                var castInput = new ColliderCastInput
                {
                    Collider = colliderPtr,
                    Orientation = orientation,
                    Start = pos,
                    End = pos + vel
                };

                if (!world.CastCollider(castInput, out var hit))
                {
                    pos = dest;
                    break;
                }

                var t = math.clamp(hit.Fraction, 0f, 1f);

                // --- Near point step (stop slightly before impact) ---
                var travelDist = remainingDist * t;
                //var shortDist = math.max(travelDist - skinWidth, 0f);
                var moveDir = math.normalizesafe(vel);

                pos += moveDir * travelDist + hit.SurfaceNormal * skinWidth;

                // --- Touch point normal (collision constraint) ---
                var planeN = hit.SurfaceNormal;

                // Register constraint plane (max 2 needed for 3 DOF in 3D)
                if (planeCount == 0)
                {
                    n1 = planeN;
                    planeCount = 1;
                }
                else if (planeCount == 1 && math.dot(planeN, n1) < 0.999f)
                {
                    n2 = planeN;
                    planeCount = 2;
                }
                else
                {
                    // Third constraint => no DOF left
                    planeCount = 3;
                }

                // --- Recompute velocity under constraints ---
                if (planeCount == 1)
                {
                    // Project onto first plane
                    vel = ProjectOnPlaneL(vel, n1);
                }
                else if (planeCount == 2)
                {
                    // Crease direction = intersection of two planes
                    var crease = math.cross(n1, n2);
                    var lenSq = math.lengthsq(crease);

                    if (lenSq < 1e-8f)
                    {
                        vel = float3.zero;
                        break;
                    }

                    crease = crease * math.rsqrt(lenSq);

                    vel = math.dot(vel, crease) * crease;
                }
                else
                {
                    vel = float3.zero;
                    break;
                }

                // Recompute destination from corrected state (prevents drift)
                dest = pos + vel;

                if (math.lengthsq(vel) < skinWidth * skinWidth)
                    break;
            }

            return pos;
        }

        private static float3 ProjectOnPlaneL(float3 v, float3 n)
        {
            return v - math.dot(v, n) * n;
        }
    }
}