using Unity.Mathematics;
using static Unity.Physics.Math;

namespace Unity.Physics.Authoring
{
    public class PrismaticJoint : BallAndSocketJoint
    {
        public float3 AxisLocal;
        public float3 AxisInConnectedEntity;
        public float3 PerpendicularAxisLocal;
        public float3 PerpendicularAxisInConnectedEntity;
        public float MinDistanceOnAxis;
        public float MaxDistanceOnAxis;

        public override void UpdateAuto()
        {
            base.UpdateAuto();
            if (AutoSetConnected)
            {
                var bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                AxisInConnectedEntity = math.mul(bFromA.rot, AxisLocal);
                PerpendicularAxisInConnectedEntity = math.mul(bFromA.rot, PerpendicularAxisLocal);
            }
        }
    }

    internal class PrismaticJointBaker : JointBaker<PrismaticJoint>
    {
        public override void Bake(PrismaticJoint authoring)
        {
            authoring.UpdateAuto();

            var physicsJoint = PhysicsJoint.CreatePrismatic(
                new BodyFrame
                {
                    Axis = authoring.AxisLocal,
                    PerpendicularAxis = authoring.PerpendicularAxisLocal,
                    Position = authoring.PositionLocal
                },
                new BodyFrame
                {
                    Axis = authoring.AxisInConnectedEntity,
                    PerpendicularAxis = authoring.PerpendicularAxisInConnectedEntity,
                    Position = authoring.PositionInConnectedEntity
                },
                new FloatRange(authoring.MinDistanceOnAxis, authoring.MaxDistanceOnAxis)
            );

            physicsJoint.SetImpulseEventThresholdAllConstraints(authoring.MaxImpulse);

            var constraintBodyPair = GetConstrainedBodyPair(authoring);

            var worldIndex = GetWorldIndexFromBaseJoint(authoring);
            CreateJointEntity(worldIndex, constraintBodyPair, physicsJoint);
        }
    }
}