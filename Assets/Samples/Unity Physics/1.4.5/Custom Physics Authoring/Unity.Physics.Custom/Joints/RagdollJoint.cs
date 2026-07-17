using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Physics.Math;

namespace Unity.Physics.Authoring
{
    public class RagdollJoint : BallAndSocketJoint
    {
        private const int k_LatestVersion = 1;

        // Editor only settings
        [HideInInspector] public bool EditAxes;

        [HideInInspector] public bool EditLimits;

        [SerializeField] private int m_Version;

        public float3 TwistAxisLocal;
        public float3 TwistAxisInConnectedEntity;
        public float3 PerpendicularAxisLocal;
        public float3 PerpendicularAxisInConnectedEntity;
        public float MaxConeAngle;
        public float MinPerpendicularAngle;
        public float MaxPerpendicularAngle;
        public float MinTwistAngle;
        public float MaxTwistAngle;

        private void OnValidate()
        {
            UpgradeVersionIfNecessary();

            MaxConeAngle = math.clamp(MaxConeAngle, 0f, 180f);

            MaxPerpendicularAngle = math.clamp(MaxPerpendicularAngle, -90f, 90f);
            MinPerpendicularAngle = math.clamp(MinPerpendicularAngle, -90f, 90f);
            if (MaxPerpendicularAngle < MinPerpendicularAngle)
            {
                var swap = new FloatRange(MinPerpendicularAngle, MaxPerpendicularAngle).Sorted();
                MinPerpendicularAngle = swap.Min;
                MaxPerpendicularAngle = swap.Max;
            }

            MinTwistAngle = math.clamp(MinTwistAngle, -180f, 180f);
            MaxTwistAngle = math.clamp(MaxTwistAngle, -180f, 180f);
            if (MaxTwistAngle < MinTwistAngle)
            {
                var swap = new FloatRange(MinTwistAngle, MaxTwistAngle).Sorted();
                MinTwistAngle = swap.Min;
                MaxTwistAngle = swap.Max;
            }
        }

        internal void UpgradeVersionIfNecessary()
        {
            if (m_Version >= k_LatestVersion)
                return;

            MinPerpendicularAngle -= 90f;
            MaxPerpendicularAngle -= 90f;
            m_Version = k_LatestVersion;
        }

        public override void UpdateAuto()
        {
            base.UpdateAuto();
            if (AutoSetConnected)
            {
                var bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                TwistAxisInConnectedEntity = math.mul(bFromA.rot, TwistAxisLocal);
                PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, PerpendicularAxisLocal);
            }
        }
    }

    internal class RagdollJointBaker : JointBaker<RagdollJoint>
    {
        public override void Bake(RagdollJoint authoring)
        {
            authoring.UpdateAuto();
            authoring.UpgradeVersionIfNecessary();

            PhysicsJoint.CreateRagdoll(
                new BodyFrame
                {
                    Axis = authoring.TwistAxisLocal, PerpendicularAxis = authoring.PerpendicularAxisLocal,
                    Position = authoring.PositionLocal
                },
                new BodyFrame
                {
                    Axis = authoring.TwistAxisInConnectedEntity,
                    PerpendicularAxis = authoring.PerpendicularAxisInConnectedEntity,
                    Position = authoring.PositionInConnectedEntity
                },
                math.radians(authoring.MaxConeAngle),
                math.radians(new FloatRange(authoring.MinPerpendicularAngle, authoring.MaxPerpendicularAngle)),
                math.radians(new FloatRange(authoring.MinTwistAngle, authoring.MaxTwistAngle)),
                out var primaryCone,
                out var perpendicularCone
            );

            primaryCone.SetImpulseEventThresholdAllConstraints(authoring.MaxImpulse);
            perpendicularCone.SetImpulseEventThresholdAllConstraints(authoring.MaxImpulse);

            var constraintBodyPair = GetConstrainedBodyPair(authoring);

            using var entities = new NativeList<Entity>(1, Allocator.TempJob);
            var worldIndex = GetWorldIndexFromBaseJoint(authoring);
            CreateJointEntities(worldIndex,
                constraintBodyPair,
                new NativeArray<PhysicsJoint>(2, Allocator.Temp) { [0] = primaryCone, [1] = perpendicularCone },
                entities);
        }
    }
}