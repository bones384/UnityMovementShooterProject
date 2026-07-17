using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    /// <summary>   The physics body authoring. This class cannot be inherited. </summary>
#if UNITY_2021_2_OR_NEWER
    [Icon(k_IconPath)]
#endif
    [AddComponentMenu("Entities/Physics/Physics Body")]
    [DisallowMultipleComponent]
    public sealed class PhysicsBodyAuthoring : MonoBehaviour
    {
        private const string k_IconPath =
            "Packages/com.unity.physics/Unity.Physics.Editor/Editor Default Resources/Icons/d_Rigidbody@64.png";

        private const float k_MinimumMass = 0.001f;

        [SerializeField]
        [Tooltip("Specifies whether the body should be fully physically simulated, moved directly, or fixed in place.")]
        private BodyMotionType m_MotionType;

        [SerializeField]
        [Tooltip(
            "Specifies how this body's motion in its graphics representation should be smoothed when the rendering framerate is greater than the fixed step rate used by physics.")]
        private BodySmoothing m_Smoothing = BodySmoothing.None;

        [SerializeField] private float m_Mass = 1.0f;

        [SerializeField] [Tooltip("This is applied to a body's linear velocity reducing it over time.")]
        private float m_LinearDamping = 0.01f;

        [SerializeField] [Tooltip("This is applied to a body's angular velocity reducing it over time.")]
        private float m_AngularDamping = 0.05f;

        [SerializeField] [Tooltip("The initial linear velocity of the body in world space")]
        private float3 m_InitialLinearVelocity = float3.zero;

        [SerializeField]
        [Tooltip(
            "This represents the initial rotation speed around each axis in the local motion space of the body i.e. around the center of mass")]
        private float3 m_InitialAngularVelocity = float3.zero;

        [SerializeField] [Tooltip("Scales the amount of gravity to apply to this body.")]
        private float m_GravityFactor = 1f;

        [SerializeField]
        [Tooltip(
            "Override default mass distribution based on the shapes associated with this body by the specified mass distribution, assuming unit mass.")]
        private bool m_OverrideDefaultMassDistribution;

        [SerializeField] private float3 m_CenterOfMass;

        [SerializeField] private EulerAngles m_Orientation = EulerAngles.Default;

        [SerializeField]
        // Default value to solid unit sphere : https://en.wikipedia.org/wiki/List_of_moments_of_inertia
        private float3 m_InertiaTensor = new(2f / 5f);

        [SerializeField]
        [Tooltip("The index of the physics world this body belongs to. Default physics world has index 0.")]
        private uint m_WorldIndex;

        [SerializeField] private CustomPhysicsBodyTags m_CustomTags = CustomPhysicsBodyTags.Nothing;

        private PhysicsBodyAuthoring()
        {
        }

        public BodyMotionType MotionType
        {
            get => m_MotionType;
            set => m_MotionType = value;
        }

        public BodySmoothing Smoothing
        {
            get => m_Smoothing;
            set => m_Smoothing = value;
        }

        public float Mass
        {
            get => m_MotionType == BodyMotionType.Dynamic ? m_Mass : float.PositiveInfinity;
            set => m_Mass = math.max(k_MinimumMass, value);
        }

        public float LinearDamping
        {
            get => m_LinearDamping;
            set => m_LinearDamping = math.max(0f, value);
        }

        public float AngularDamping
        {
            get => m_AngularDamping;
            set => m_AngularDamping = math.max(0f, value);
        }

        public float3 InitialLinearVelocity
        {
            get => m_InitialLinearVelocity;
            set => m_InitialLinearVelocity = value;
        }

        public float3 InitialAngularVelocity
        {
            get => m_InitialAngularVelocity;
            set => m_InitialAngularVelocity = value;
        }

        public float GravityFactor
        {
            get => m_MotionType == BodyMotionType.Dynamic ? m_GravityFactor : 0f;
            set => m_GravityFactor = value;
        }

        public bool OverrideDefaultMassDistribution
        {
#pragma warning disable 618
            get => m_OverrideDefaultMassDistribution;
            set => m_OverrideDefaultMassDistribution = value;
#pragma warning restore 618
        }

        public MassDistribution CustomMassDistribution
        {
            get => new()
            {
                Transform = new RigidTransform(m_Orientation, m_CenterOfMass),
                InertiaTensor =
                    m_MotionType == BodyMotionType.Dynamic ? m_InertiaTensor : new float3(float.PositiveInfinity)
            };
            set
            {
                m_CenterOfMass = value.Transform.pos;
                m_Orientation.SetValue(value.Transform.rot);
                m_InertiaTensor = value.InertiaTensor;
#pragma warning disable 618
                m_OverrideDefaultMassDistribution = true;
#pragma warning restore 618
            }
        }

        public uint WorldIndex
        {
            get => m_WorldIndex;
            set => m_WorldIndex = value;
        }

        public CustomPhysicsBodyTags CustomTags
        {
            get => m_CustomTags;
            set => m_CustomTags = value;
        }

        private void OnEnable()
        {
            // included so tick box appears in Editor
        }

        private void OnValidate()
        {
            m_Mass = math.max(k_MinimumMass, m_Mass);
            m_LinearDamping = math.max(m_LinearDamping, 0f);
            m_AngularDamping = math.max(m_AngularDamping, 0f);
        }
    }
}