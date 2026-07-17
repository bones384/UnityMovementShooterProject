using Entities.Movement;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using Collider = Unity.Physics.Collider;

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
        public float airControlFactor;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<EntitiesReferences>();
            state.RequireForUpdate<PlayerInput>();
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            maxSpeed = entitiesReferences.MaxSpeed;
            acceleration = entitiesReferences.Acceleration;
            jumpSpeed = entitiesReferences.JumpSpeed;
            initialSpeed = entitiesReferences.InitialSpeed;
            gravity = math.abs(entitiesReferences.Gravity);
            dampenSpeed = entitiesReferences.DampenSpeed;
            maxFallSpeed = entitiesReferences.MaxFallSpeed;
            airControlFactor = entitiesReferences.AirControlFactor;

            var wallRunMaxTime = entitiesReferences.WallRunMaxTime;
            var wallRunDrag = entitiesReferences.WallRunDrag;
            var wallRunMinSpeed = entitiesReferences.WallRunMinSpeed;
            var wallJumpBoost = entitiesReferences.WallJumpBoost;
            var applyWallGravity = entitiesReferences.ApplyWallGravity;
            var wallGravityMultiplier = entitiesReferences.WallGravityMultiplier;

            var dt = SystemAPI.Time.DeltaTime;
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            foreach (var (playerInput, localTransform, playerLook, pstate, contacts) in SystemAPI
                         .Query<RefRO<PlayerInput>, RefRW<LocalTransform>, RefRW<PlayerLook>,
                             RefRW<PlayerStateComponent>, RefRO<MovementRaycasterComponent>>()
                         .WithAll<Simulate>())
            {
                var lookVector = playerInput.ValueRO.InputLookVector;
                const float mouseSensitivity = 1f;
                lookVector *= mouseSensitivity * dt;

                playerLook.ValueRW.Pitch =
                    math.clamp(playerLook.ValueRW.Pitch + lookVector.y, -math.PI / 2, math.PI / 2);
                playerLook.ValueRW.Yaw = math.fmod(playerLook.ValueRW.Yaw + lookVector.x, 2 * math.PI);

                var cameraRotation = math.mul(quaternion.RotateY(playerLook.ValueRW.Yaw), quaternion.RotateX(0));
                localTransform.ValueRW.Rotation = cameraRotation;

                var inputMovement = playerInput.ValueRO.InputMovementVector;
                var move = localTransform.ValueRO.Right() * inputMovement.x +
                           localTransform.ValueRO.Forward() * inputMovement.y;
                move = math.normalizesafe(move);
                var isJumpButtonHeld = playerInput.ValueRO.JumpInput;

                if (pstate.ValueRO.IsDead)
                {
                    move = float3.zero;
                    isJumpButtonHeld = false;
                }

                var isGrounded = contacts.ValueRO.IsGrounded;
                var currentVelocity = pstate.ValueRO.Velocity;

                if (currentVelocity.y > 0.1f) isGrounded = false;

                var horizontalVelocity = new float3(currentVelocity.x, 0, currentVelocity.z);
                var verticalVelocity = new float3(0, currentVelocity.y, 0);

                var inputTowardsLeftWall = inputMovement.x < -0.1f;
                var inputTowardsRightWall = inputMovement.x > 0.1f;

                var isAlreadyRunning = pstate.ValueRO.IsWallRunning;

                var pullingAwayFromLeft = inputMovement.x > 0.1f;
                var pullingAwayFromRight = inputMovement.x < -0.1f;


                var keepRunningLeft = isAlreadyRunning && contacts.ValueRO.HasWallLeft
                                                       && math.dot(contacts.ValueRO.WallLeftNormal,
                                                           pstate.ValueRO.LastWallNormal) > 0.1f
                                                       && !pullingAwayFromLeft;

                var keepRunningRight = isAlreadyRunning && contacts.ValueRO.HasWallRight
                                                        && math.dot(contacts.ValueRO.WallRightNormal,
                                                            pstate.ValueRO.LastWallNormal) > 0.1f
                                                        && !pullingAwayFromRight;

                var canWallRunLeft = (contacts.ValueRO.HasWallLeft && inputTowardsLeftWall) || keepRunningLeft;
                var canWallRunRight = (contacts.ValueRO.HasWallRight && inputTowardsRightWall) || keepRunningRight;

                var currentWallNormal = float3.zero;
                if (canWallRunLeft) currentWallNormal = contacts.ValueRO.WallLeftNormal;
                else if (canWallRunRight) currentWallNormal = contacts.ValueRO.WallRightNormal;

                var isNearWall = canWallRunLeft || canWallRunRight;

                var isSameWall = math.dot(currentWallNormal, pstate.ValueRO.LastWallNormal) > 0.9f;

                var isValidWall = !isSameWall || isAlreadyRunning;
                var shouldWallRun = !isGrounded && isNearWall && isValidWall;

                if (isGrounded) pstate.ValueRW.LastWallNormal = float3.zero;

                if (isGrounded)
                    pstate.ValueRW.CoyoteTimer = 0.15f;
                else if (pstate.ValueRO.CoyoteTimer > 0) pstate.ValueRW.CoyoteTimer -= dt;

                var wasJumpButtonHeldLastTick = pstate.ValueRO.IsJumping;

                if (isJumpButtonHeld && !wasJumpButtonHeldLastTick) pstate.ValueRW.JumpBufferTimer = 0.15f;
                if (pstate.ValueRO.JumpBufferTimer > 0) pstate.ValueRW.JumpBufferTimer -= dt;

                if (pstate.ValueRO.JumpBufferTimer > 0)
                {
                    if (pstate.ValueRO.CoyoteTimer > 0) // Ground Jump
                    {
                        verticalVelocity.y = jumpSpeed;
                        isGrounded = false;
                        pstate.ValueRW.JumpBufferTimer = 0;
                        pstate.ValueRW.CoyoteTimer = 0;
                    }
                    else if (shouldWallRun || pstate.ValueRO.IsWallRunning) // Wall Jump
                    {
                        verticalVelocity.y = jumpSpeed;

                        var currentForward = math.normalizesafe(horizontalVelocity);

                        if (math.lengthsq(horizontalVelocity) < 0.1f) currentForward = localTransform.ValueRO.Forward();

                        // This creates a vector pointing forward AND away from the wall
                        var jumpOffDir = math.normalizesafe(currentForward + currentWallNormal);

                        var currentSpeed = math.max(math.length(horizontalVelocity), initialSpeed);
                        horizontalVelocity = jumpOffDir * (currentSpeed * wallJumpBoost);

                        isGrounded = false;
                        shouldWallRun = false;
                        pstate.ValueRW.IsWallRunning = false;

                        pstate.ValueRW.LastWallNormal = currentWallNormal;
                        pstate.ValueRW.JumpBufferTimer = 0;
                    }
                }

                pstate.ValueRW.IsGrounded = isGrounded;

                if (isGrounded)
                {
                    pstate.ValueRW.IsWallRunning = false;
                    verticalVelocity.y = 0;

                    if (math.lengthsq(move) > 0)
                    {
                        var currentSpeed = math.length(horizontalVelocity);

                        if (currentSpeed < initialSpeed)
                            currentSpeed = initialSpeed;
                        else if (currentSpeed < maxSpeed)
                            currentSpeed = math.min(maxSpeed, currentSpeed + acceleration * dt);
                        else if (currentSpeed > maxSpeed)
                            currentSpeed = math.max(maxSpeed, currentSpeed - dampenSpeed * dt);

                        horizontalVelocity = move * currentSpeed;
                    }
                    else
                    {
                        var currentSpeed = math.length(horizontalVelocity);
                        currentSpeed = math.max(0, currentSpeed - 1000 * dampenSpeed * dt);

                        if (currentSpeed > 0)
                            horizontalVelocity = math.normalizesafe(horizontalVelocity) * currentSpeed;
                        else
                            horizontalVelocity = float3.zero;
                    }
                }
                else if (shouldWallRun)
                {
                    if (!pstate.ValueRO.IsWallRunning)
                    {
                        pstate.ValueRW.IsWallRunning = true;
                        pstate.ValueRW.WallRunTimer = wallRunMaxTime;

                        verticalVelocity.y = 0f;

                        if (math.length(horizontalVelocity) < initialSpeed)
                            horizontalVelocity = math.normalizesafe(horizontalVelocity) * initialSpeed;
                    }

                    pstate.ValueRW.LastWallNormal = currentWallNormal;

                    var upVector = new float3(0, 1, 0);
                    var wallForward = math.cross(currentWallNormal, upVector);

                    if (math.dot(wallForward, localTransform.ValueRO.Forward()) < 0) wallForward = -wallForward;

                    var velocityAlongWall = horizontalVelocity -
                                            math.dot(horizontalVelocity, currentWallNormal) * currentWallNormal;
                    var currentSpeed = math.length(velocityAlongWall);

                    pstate.ValueRW.WallRunTimer -= dt;

                    if (pstate.ValueRO.WallRunTimer <= 0) currentSpeed -= wallRunDrag * dt;

                    if (currentSpeed < wallRunMinSpeed)
                    {
                        shouldWallRun = false;
                        pstate.ValueRW.IsWallRunning = false;
                    }
                    else
                    {
                        var stickForce = -currentWallNormal * 3f;
                        var targetVelocity = wallForward * currentSpeed + stickForce;

                        horizontalVelocity = math.lerp(horizontalVelocity, targetVelocity, math.saturate(15f * dt));

                        if (applyWallGravity)
                            verticalVelocity.y -= gravity * wallGravityMultiplier * dt;
                        else
                            verticalVelocity.y = 0;
                    }
                }

                if (!isGrounded && !shouldWallRun)
                {
                    pstate.ValueRW.IsWallRunning = false;

                    verticalVelocity.y -= gravity * dt;
                    verticalVelocity.y = math.max(-maxFallSpeed, verticalVelocity.y);

                    if (math.lengthsq(move) > 0)
                    {
                        var currentSpeed = math.length(horizontalVelocity);

                        if (currentSpeed < initialSpeed)
                            currentSpeed = initialSpeed;
                        else if (currentSpeed < maxSpeed)
                            currentSpeed = math.min(maxSpeed, currentSpeed + acceleration * dt);

                        var targetVelocity = move * currentSpeed;

                        horizontalVelocity = math.lerp(
                            horizontalVelocity,
                            targetVelocity,
                            math.saturate(15f * airControlFactor * dt)
                        );
                    }
                }

// --- 1. Freeze physical body in the abyss ---
                if (pstate.ValueRO.IsDead)
                {
                    horizontalVelocity = float3.zero;
                    verticalVelocity = float3.zero;
                }

                pstate.ValueRW.Velocity = horizontalVelocity + verticalVelocity;

                pstate.ValueRW.IsJumping = isJumpButtonHeld;

                var horizontalDisplacement = horizontalVelocity * dt;
                var verticalDisplacement = verticalVelocity * dt;

                var filter = new CollisionFilter
                {
                    BelongsTo = 1u << 7,
                    CollidesWith = 1u << 6,
                    GroupIndex = 0
                };

                var halfSegment = math.max(0f, 1.8f - 2f * 0.45f) * 0.5f;
                var capsuleGeometry = new CapsuleGeometry
                {
                    Radius = 0.45f,
                    Vertex0 = new float3(0, 1, 0) + new float3(0, -halfSegment, 0),
                    Vertex1 = new float3(0, 1, 0) + new float3(0, halfSegment, 0)
                };
                var capsuleCollider = CapsuleCollider.Create(capsuleGeometry, filter);

                var pos = localTransform.ValueRO.Position;

                CollideAndSlide_Linahan(collisionWorld, capsuleCollider,
                    pos, localTransform.ValueRO.Rotation,
                    horizontalDisplacement, filter, isGrounded, false,
                    out pos, 3, 0.001f);

                CollideAndSlide_Linahan(collisionWorld, capsuleCollider,
                    pos, localTransform.ValueRO.Rotation,
                    verticalDisplacement, filter, isGrounded, true,
                    out pos, 3, 0.001f);

                localTransform.ValueRW.Position = pos;
                pstate.ValueRW.Rotation = cameraRotation;
                capsuleCollider.Dispose();
            }

            foreach (var (playerInput, localTransform, playerLook, playerState) in SystemAPI
                         .Query<RefRO<PlayerInput>, RefRO<LocalTransform>, RefRO<PlayerLook>,
                             RefRW<PlayerStateComponent>>())
            {
                playerState.ValueRW.Position = localTransform.ValueRO.Position;
                playerState.ValueRW.Rotation = localTransform.ValueRO.Rotation;
            }
        }

        [BurstCompile]
        public static unsafe void CollideAndSlide_Linahan(
            in CollisionWorld world,
            in BlobAssetReference<Collider> capsuleCollider,
            in float3 position,
            in quaternion orientation,
            in float3 velocity,
            in CollisionFilter filter,
            bool isGrounded,
            bool gravityPass,
            out float3 newPosition,
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
                    // if (isGrounded && !gravityPass)
                    //   vel = ProjectOnPlaneL(new float3(vel.x, 0, vel.z), new float3(n1.x, 0, n1.z));

                    // else
                    ProjectOnPlaneL(vel, n1, out vel);
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

            newPosition = pos;
        }

        // [BurstCompile]
        private static void ProjectOnPlaneL(in float3 v, in float3 n, out float3 res)
        {
            res = v - math.dot(v, n) * n;
        }
    }
}