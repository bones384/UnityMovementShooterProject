using Unity.Mathematics;

namespace Unity.Physics.Authoring
{
    public class RigidJoint : BallAndSocketJoint
    {
        public quaternion OrientationLocal = quaternion.identity;
        public quaternion OrientationInConnectedEntity = quaternion.identity;

        public override void UpdateAuto()
        {
            base.UpdateAuto();
            if (AutoSetConnected)
            {
                var bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                OrientationInConnectedEntity = math.mul(bFromA.rot, OrientationLocal);
            }

            {
                OrientationLocal = math.normalize(OrientationLocal);
                OrientationInConnectedEntity = math.normalize(OrientationInConnectedEntity);
            }
        }
    }

    internal class RigidJointBaker : JointBaker<RigidJoint>
    {
        public override void Bake(RigidJoint authoring)
        {
            authoring.UpdateAuto();

            var physicsJoint = PhysicsJoint.CreateFixed(
                new RigidTransform(authoring.OrientationLocal, authoring.PositionLocal),
                new RigidTransform(authoring.OrientationInConnectedEntity, authoring.PositionInConnectedEntity)
            );

            physicsJoint.SetImpulseEventThresholdAllConstraints(authoring.MaxImpulse);

            var constraintBodyPair = GetConstrainedBodyPair(authoring);

            var worldIndex = GetWorldIndexFromBaseJoint(authoring);
            CreateJointEntity(worldIndex, constraintBodyPair, physicsJoint);
        }
    }
}