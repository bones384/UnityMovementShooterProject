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
    internal partial struct PlayerMovementSystem :
        ISystem
    {
        public float maxSpeed;
        public float acceleration;
        public float jumpSpeed;
        public float initialSpeed;
        public float gravity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<EntitiesReferences>();
            state.RequireForUpdate<PlayerInput>();
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

            maxSpeed = entitiesReferences.MaxSpeed;
            acceleration = entitiesReferences.Acceleration;
            jumpSpeed = entitiesReferences.JumpSpeed;
            initialSpeed = entitiesReferences.InitialSpeed;
            gravity = entitiesReferences.Gravity;

            var time = SystemAPI.GetSingleton<NetworkTime>();

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
                var forward = math.mul(cameraRotation, math.forward());

                playerLook.ValueRW.Pitch =
                    math.clamp(playerLook.ValueRW.Pitch + lookVector.y, -math.PI / 2, math.PI / 2);
                playerLook.ValueRW.Yaw = math.fmod(playerLook.ValueRW.Yaw + lookVector.x, 2 * math.PI);

                var moveSpeed = 4f;
                var move =
                    localTransform.ValueRO.Right() * playerInput.ValueRO.InputMovementVector.x +
                    localTransform.ValueRO.Forward() * playerInput.ValueRO.InputMovementVector.y;

                move = math.normalizesafe(move);
//Debug.Log($"move: {move} forward: {forward} moveSpeed: {moveSpeed}");
                var displacement = move * moveSpeed * SystemAPI.Time.DeltaTime;
                //if(displacement.Equals(float3.zero)) continue;

                if (!contacts.ValueRO.IsGrounded)
                    displacement += new float3(0, 1, 0) * gravity * SystemAPI.Time.DeltaTime;
                else
                    localTransform.ValueRW.Position = contacts.ValueRO.GroundHit;
                var res = collideAndSlide(displacement, localTransform.ValueRO.Position + new float3(0, 1, 0), 5);
//2 var res = cas(displacement, localTransform.ValueRO.Position + new float3(0,1,0), 0);

                DebugVector.DrawArrow(localTransform.ValueRO.Position + new float3(0, 1, 0), res, Color.purple);
                localTransform.ValueRW.Position += res;
//3
//localTransform.ValueRW.Position = collideWithWorld(localTransform.ValueRO.Position + new float3(0, 1, 0), displacement) - new float3(0, 1, 0);
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


        public unsafe Entity SCast(float3 RayFrom, float3 RayTo, BlobAssetReference<Collider> radius, out bool haveHit,
            out ColliderCastHit hit)
        {
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            var filter = new CollisionFilter
            {
                BelongsTo = 1u << 7, // Is on player layer
                CollidesWith = 1u << 6, // Raycast against level layer
                GroupIndex = 0
            };


            var sphereGeometry = new SphereGeometry { Center = float3.zero, Radius = 0.2f };
            var halfSegment = math.max(0f, 1.8f - 2f * 0.45f) * 0.5f;

            var capsuleGeometry = new CapsuleGeometry
            {
                Radius = 0.45f,
                Vertex0 = new float3(0, 1, 0) + new float3(0, -halfSegment, 0),
                Vertex1 = new float3(0, 1, 0) + new float3(0, halfSegment, 0)
            };
            var sphereCollider = SphereCollider.Create(sphereGeometry, filter);
            var capsuleCollider = CapsuleCollider.Create(capsuleGeometry, filter);
            var input = new ColliderCastInput
            {
                Start = RayFrom,
                End = RayTo,
                Collider = (Collider*)capsuleCollider.GetUnsafePtr(),
                Orientation = quaternion.identity
            };
            haveHit = collisionWorld.CastCollider(input, out hit);

            Debug.Log($"Hit: {haveHit} Entity: {hit.Entity}");
            Debug.DrawLine(RayFrom, RayTo, Color.blueViolet);
            DebugVector.DrawArrow(RayFrom, RayTo - RayFrom, Color.green);
            sphereCollider.Dispose();

            if (haveHit)
            {
                //  Debug.DrawLine(RayFrom, hit.Position, Color.red);
                DebugVector.DrawArrow(RayFrom, hit.Position - RayFrom, Color.red);
                return hit.Entity;
            }


            return Entity.Null;

            /*


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
                haveHit = collisionWorld.CastCollider(input, out hit);

                Debug.Log($"Hit: {haveHit} From: {RayFrom} To: {RayTo} Entity: {hit.Entity}");
                Debug.DrawLine(RayFrom, RayTo, Color.green);
                Debug.DrawLine(RayFrom, hit.Position, Color.red);
                return hit;

            }
            */
        }

        public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
        {
            var sqrMag = math.dot(planeNormal, planeNormal);
            if (sqrMag < Mathf.Epsilon)
                return vector;
            var dot = math.dot(vector, planeNormal);
            return new float3(vector.x - planeNormal.x * dot / sqrMag,
                vector.y - planeNormal.y * dot / sqrMag,
                vector.z - planeNormal.z * dot / sqrMag);
        }

        private const float skinWidth = 0.1f;
        private const float clearance = 0.1f;

        private float3 collideAndSlide(float3 inputDisplacement, float3 pos, int maxDepth)
        {
            // cast a bit from inside the collider to avoid going through walls
            //  pos -= math.normalizesafe(inputDisplacement) * skinWidth;
            // increase displacement to compensate
            // inputDisplacement += math.normalizesafe(inputDisplacement) * skinWidth;

            var finalDisplacement = float3.zero;
            for (var i = 0; i < maxDepth; i++)
            {
                var collision = SCast(pos, pos + inputDisplacement + clearance, default, out var haveHit, out var hit);
                if (!haveHit)
                {
                    // If nothing is hit, we can move the full distance and stop there
                    finalDisplacement += inputDisplacement;
                    break;
                }

                // Move until you're almost hitting the wall
                var forward = math.normalizesafe(inputDisplacement);
                var vel = inputDisplacement * hit.Fraction +
                          clearance * hit.SurfaceNormal; // how much we can move in this direction
                pos += vel; //advance position

                // Calculate the remaining displacement to move along the wall
                var remainingMove = inputDisplacement * (1 - hit.Fraction);

                if (math.length(vel) <= skinWidth)
                {
                    //     vel = 0;
                }

                remainingMove = ProjectOnPlane(remainingMove, hit.SurfaceNormal);

                //remainingMove = math.normalizesafe(remainingMove);
                // remainingMove *=  math.length(remainingMove);

                inputDisplacement = remainingMove;

                finalDisplacement += vel;
            }

            return finalDisplacement;
        }

        private static readonly int maxbounces = 5;

        private float3 cas(float3 vel, float3 pos, int depth)
        {
            var skin = 0.015f;
            if (depth >= maxbounces) return float3.zero;

            var dist = math.length(vel);
            var collision = SCast(pos, pos + vel, default, out var haveHit, out var hit);
            if (haveHit)
            {
                //float hitDistance = math.length(hit.Position - pos);
                var hitDistance = math.length(hit.Fraction * vel);
                // or hit.Fraction * vel.mag?
                //float3 snapToSurface = (math.normalize(vel) * (hitDistance)) + (hit.SurfaceNormal * skin);
                var snapToSurface = hitDistance + hit.SurfaceNormal * skin;
                var leftover = vel - snapToSurface;

                if (math.length(snapToSurface) < skin) snapToSurface = float3.zero;

                var mag = math.length(leftover);
                leftover = ProjectOnPlane(leftover, hit.SurfaceNormal);
                //leftover = math.normalize(leftover) * mag;

                return snapToSurface + cas(leftover, pos + snapToSurface, depth + 1);
            }

            return vel;
        }


        //class PLANE {
//public:
//float equation[4];
//VECTOR origin;
//VECTOR normal;

        private class PLANE
        {
            public readonly float4 equation;
            public readonly float3 normal;
            public float3 origin;

//PLANE::PLANE(const VECTOR& origin, const VECTOR& normal) {
//this->normal = normal;
//this->origin = origin;
//equation[0] = normal.x;
//equation[1] = normal.y;
//equation[2] = normal.z;
//equation[3] = -(normal.x*origin.x+normal.y*origin.y
//+normal.z*origin.z);
//}

            public PLANE(float3 origin, float3 normal)
            {
                this.normal = normal;
                this.origin = origin;
                equation = new float4(normal.x, normal.y, normal.z,
                    -(normal.x * origin.x + normal.y * origin.y + normal.z * origin.z));
            }

//bool PLANE::isFrontFacingTo(const VECTOR& direction) const {
//double dot = normal.dot(direction);
//return (dot <= 0);

            public bool isFrontFacingTo(float3 direction)
            {
                var dot = math.dot(normal, direction);
                return dot <= 0;
            }


//double PLANE::signedDistanceTo(const VECTOR& point) const {
//return (point.dot(normal)) + equation[3];
//}
//};

            public float signedDistanceTo(float3 point)
            {
                return math.dot(point, normal) + equation.w;
            }
        }


//// Set this to match application scale..
//const float unitsPerMeter = 100.0f;
        private const float unitsPerMeter = 1;

//VECTOR CharacterEntity::collideWithWorld(const VECTOR& pos,
//const VECTOR& vel)
//{

        private float3 collideWithWorld(float3 pos, float3 vel, int collisionRecursionDepth = 0)
        {
//// All hard-coded distances in this function is
//// scaled to fit the setting above..
//float unitScale = unitsPerMeter / 100.0f;
//float veryCloseDistance = 0.005f * unitScale;
            var unitScale = unitsPerMeter / 100.0f;
            var veryCloseDistance = 0.015f * unitScale;


//// do we need to worry?
//if (collisionRecursionDepth>5)
//return pos;

            if (collisionRecursionDepth > 5) return pos;

//// Ok, we need to worry:
//collisionPackage->velocity = vel;
//45
//collisionPackage->normalizedVelocity = vel;
//collisionPackage->normalizedVelocity.normalize();
//collisionPackage->basePoint = pos;
//collisionPackage->foundCollision = false;
//// Check for collision (calls the collision routines)
//// Application specific!!
//world->checkCollision(collisionPackage);

            var collision = SCast(pos, pos + vel, default, out var haveHit, out var hit);


//// If no collision we just move along the velocity
//if (collisionPackage->foundCollision == false) {
//return pos + vel;
//}

            if (!haveHit) return pos + vel;
            var intersectionPoint = hit.Position;

//// *** Collision occured ***
//// The original destination point
//VECTOR destinationPoint = pos + vel;
//VECTOR newBasePoint = pos;

            var destinationPoint = pos + vel;
            var newBasePoint = pos;

//// only update if we are not already very close
//// and if so we only move very close to intersection..not
//// to the exact spot.
//if (collisionPackage->nearestDistance>=veryCloseDistance)
//{
            var nearestDistance = math.length(hit.Fraction * vel);
            if (nearestDistance >= veryCloseDistance)
            {
//VECTOR V = vel;
                var V = vel;
//V.SetLength(collisionPackage->nearestDistance-
//veryCloseDistance);

                V = math.normalize(V) * nearestDistance;

//newBasePoint = collisionPackage->basePoint + V;

                newBasePoint = pos + V;

//// Adjust polygon intersection point (so sliding
//// plane will be unaffected by the fact that we
//// move slightly less than collision tells us)

//V.normalize();
                V = math.normalize(V);

//collisionPackage->intersectionPoint -=
//veryCloseDistance * V;
//}

                intersectionPoint -= veryCloseDistance * V;
            }

//// Determine the sliding plane
//VECTOR slidePlaneOrigin =
//collisionPackage->intersectionPoint;

            var slidePlaneOrigin = intersectionPoint;

//VECTOR slidePlaneNormal =
//newBasePoint-collisionPackage->intersectionPoint;

            var slidePlaneNormal = newBasePoint - intersectionPoint;

//slidePlaneNormal.normalize();

            slidePlaneNormal = math.normalize(slidePlaneNormal);

//PLANE slidingPlane(slidePlaneOrigin,slidePlaneNormal);

            var slidingPlane = new PLANE(slidePlaneOrigin, slidePlaneNormal);

//// Again, sorry about formatting.. but look carefully ;)
//VECTOR newDestinationPoint = destinationPoint -
//slidingPlane.signedDistanceTo(destinationPoint)*
//slidePlaneNormal;

            var newDestinationPoint =
                destinationPoint - slidingPlane.signedDistanceTo(destinationPoint) * slidePlaneNormal;

//// Generate the slide vector, which will become our new
//// velocity vector for the next iteration
//VECTOR newVelocityVector = newDestinationPoint -
//collisionPackage->intersectionPoint;

            var newVelocityVector = newDestinationPoint - intersectionPoint;

//// Recurse:
//// dont recurse if the new velocity is very small
//if (newVelocityVector.length() < veryCloseDistance) {
//return newBasePoint;
//}

            if (math.length(newVelocityVector) < veryCloseDistance) return newBasePoint;

//collisionRecursionDepth++;
//return collideWithWorld(newBasePoint,newVelocityVector);
//}

            return collideWithWorld(newBasePoint, newVelocityVector, collisionRecursionDepth + 1);
        }
    }
}