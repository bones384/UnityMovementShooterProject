using System;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    internal interface IPhysicsMaterialProperties
    {
        PhysicsMaterialFlag DetailedStaticMeshCollision { get; set; }

        CollisionResponsePolicy CollisionResponse { get; set; }

        PhysicsMaterialCoefficient Friction { get; set; }

        PhysicsMaterialCoefficient Restitution { get; set; }

        PhysicsCategoryTags BelongsTo { get; set; }

        PhysicsCategoryTags CollidesWith { get; set; }

        // TODO: Enable Mass Factors?
        // TODO: Surface Velocity?

        CustomPhysicsMaterialTags CustomTags { get; set; }
    }

    internal interface IInheritPhysicsMaterialProperties : IPhysicsMaterialProperties
    {
        PhysicsMaterialTemplate Template { get; set; }
        bool OverrideDetailedStaticMeshCollision { get; set; }
        bool OverrideCollisionResponse { get; set; }
        bool OverrideFriction { get; set; }
        bool OverrideRestitution { get; set; }
        bool OverrideBelongsTo { get; set; }
        bool OverrideCollidesWith { get; set; }
        bool OverrideCustomTags { get; set; }
    }

    [Serializable]
    public struct PhysicsMaterialFlag
    {
        public bool Value;
    }

    [Serializable]
    public struct PhysicsMaterialCoefficient
    {
        [SoftRange(0f, 1f, TextFieldMax = float.MaxValue)]
        public float Value;

        public Material.CombinePolicy CombineMode;
    }

    internal abstract class OverridableValue<T> where T : struct
    {
        [SerializeField] private bool m_Override;
        [SerializeField] private T m_Value;

        public bool Override
        {
            get => m_Override;
            set => m_Override = value;
        }

        public T Value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                Override = true;
            }
        }

        public void OnValidate()
        {
            OnValidate(ref m_Value);
        }

        protected virtual void OnValidate(ref T value)
        {
        }
    }

    [Serializable]
    internal class OverridableCollisionResponse : OverridableValue<CollisionResponsePolicy>
    {
    }

    [Serializable]
    internal class OverridableMaterialCoefficient : OverridableValue<PhysicsMaterialCoefficient>
    {
        protected override void OnValidate(ref PhysicsMaterialCoefficient value)
        {
            value.Value = math.max(0f, value.Value);
        }
    }

    [Serializable]
    internal class OverridablePhysicsMaterialFlag : OverridableValue<PhysicsMaterialFlag>
    {
    }

    [Serializable]
    internal class OverridableCategoryTags : OverridableValue<PhysicsCategoryTags>
    {
    }

    [Serializable]
    internal class OverridableCustomMaterialTags : OverridableValue<CustomPhysicsMaterialTags>
    {
    }

    [Serializable]
    internal class PhysicsMaterialProperties : IInheritPhysicsMaterialProperties, ISerializationCallbackReceiver
    {
        private const int k_LatestVersion = 1;

        internal static bool s_SuppressUpgradeWarnings;

        [SerializeField] [HideInInspector] private bool m_SupportsTemplate;

        [SerializeField] [Tooltip("Assign a template to use its values.")]
        private PhysicsMaterialTemplate m_Template;

        [SerializeField] private OverridableCollisionResponse m_CollisionResponse = new()
        {
            Value = CollisionResponsePolicy.Collide,
            Override = false
        };

        [SerializeField] private OverridablePhysicsMaterialFlag m_DetailedStaticMeshCollision = new()
        {
            Value = new PhysicsMaterialFlag { Value = false },
            Override = false
        };

        [SerializeField] private OverridableMaterialCoefficient m_Friction = new()
        {
            Value = new PhysicsMaterialCoefficient { Value = 0.5f, CombineMode = Material.CombinePolicy.GeometricMean },
            Override = false
        };

        [SerializeField] private OverridableMaterialCoefficient m_Restitution = new()
        {
            Value = new PhysicsMaterialCoefficient { Value = 0f, CombineMode = Material.CombinePolicy.Maximum },
            Override = false
        };

        [SerializeField] private OverridableCategoryTags m_BelongsToCategories =
            new() { Value = PhysicsCategoryTags.Everything, Override = false };

        [SerializeField] private OverridableCategoryTags m_CollidesWithCategories =
            new() { Value = PhysicsCategoryTags.Everything, Override = false };

        [SerializeField] private OverridableCustomMaterialTags m_CustomMaterialTags =
            new() { Value = default, Override = false };

        [SerializeField] private int m_SerializedVersion;

        public PhysicsMaterialProperties(bool supportsTemplate)
        {
            m_SupportsTemplate = supportsTemplate;
        }

        public PhysicsMaterialTemplate Template
        {
            get => m_Template;
            set => m_Template = m_SupportsTemplate ? value : null;
        }

        public bool OverrideCollisionResponse
        {
            get => m_CollisionResponse.Override;
            set => m_CollisionResponse.Override = value;
        }

        public CollisionResponsePolicy CollisionResponse
        {
            get => Get(m_CollisionResponse, m_Template == null ? null : m_Template?.CollisionResponse);
            set => m_CollisionResponse.Value = value;
        }

        public bool OverrideDetailedStaticMeshCollision
        {
            get => m_DetailedStaticMeshCollision.Override;
            set => m_DetailedStaticMeshCollision.Override = value;
        }

        public PhysicsMaterialFlag DetailedStaticMeshCollision
        {
            get => Get(m_DetailedStaticMeshCollision,
                m_Template == null ? null : m_Template?.DetailedStaticMeshCollision);
            set => m_DetailedStaticMeshCollision.Value = value;
        }

        public bool OverrideFriction
        {
            get => m_Friction.Override;
            set => m_Friction.Override = value;
        }

        public PhysicsMaterialCoefficient Friction
        {
            get => Get(m_Friction, m_Template == null ? null : m_Template?.Friction);
            set => m_Friction.Value = value;
        }

        public bool OverrideRestitution
        {
            get => m_Restitution.Override;
            set => m_Restitution.Override = value;
        }

        public PhysicsMaterialCoefficient Restitution
        {
            get => Get(m_Restitution, m_Template == null ? null : m_Template?.Restitution);
            set => m_Restitution.Value = value;
        }

        public bool OverrideBelongsTo
        {
            get => m_BelongsToCategories.Override;
            set => m_BelongsToCategories.Override = value;
        }

        public PhysicsCategoryTags BelongsTo
        {
            get => Get(m_BelongsToCategories, m_Template == null ? null : m_Template?.BelongsTo);
            set => m_BelongsToCategories.Value = value;
        }

        public bool OverrideCollidesWith
        {
            get => m_CollidesWithCategories.Override;
            set => m_CollidesWithCategories.Override = value;
        }

        public PhysicsCategoryTags CollidesWith
        {
            get => Get(m_CollidesWithCategories, m_Template == null ? null : m_Template?.CollidesWith);
            set => m_CollidesWithCategories.Value = value;
        }

        public bool OverrideCustomTags
        {
            get => m_CustomMaterialTags.Override;
            set => m_CustomMaterialTags.Override = value;
        }

        public CustomPhysicsMaterialTags CustomTags
        {
            get => Get(m_CustomMaterialTags, m_Template == null ? null : m_Template?.CustomTags);
            set => m_CustomMaterialTags.Value = value;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            UpgradeVersionIfNecessary();
        }

        private static T Get<T>(OverridableValue<T> value, T? templateValue) where T : struct
        {
            return value.Override || templateValue == null ? value.Value : templateValue.Value;
        }

        internal static void OnValidate(ref PhysicsMaterialProperties material, bool supportsTemplate)
        {
            material.UpgradeVersionIfNecessary();

            material.m_SupportsTemplate = supportsTemplate;
            if (!supportsTemplate)
            {
                material.m_Template = null;
                material.m_CollisionResponse.Override = true;
                material.m_Friction.Override = true;
                material.m_Restitution.Override = true;
            }

            material.m_Friction.OnValidate();
            material.m_Restitution.OnValidate();
        }

#pragma warning disable 618
        private void UpgradeVersionIfNecessary()
        {
            if (m_SerializedVersion < k_LatestVersion)
                // old data from version < 1 have been removed
                if (m_SerializedVersion < 1)
                    m_SerializedVersion = 1;
        }

#pragma warning restore 618
    }
}