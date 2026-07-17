using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode.Shooting
{
    public class TurretAuthoring : MonoBehaviour
    {
        public float FireInterval = 2.0f;
        public float ProjectileSpeed = 30f;
        public float ProjectileDamage = 20f;
        public float ProjectileLifespan = 10f;
        public bool ProjectileApplyGravity;
        public float ProjectileRadius = 0.5f;

        private class Baker : Baker<TurretAuthoring>
        {
            public override void Bake(TurretAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TurretComponent
                {
                    FireInterval = authoring.FireInterval,
                    Timer = authoring.FireInterval,
                    ProjectileSpeed = authoring.ProjectileSpeed,
                    ProjectileDamage = authoring.ProjectileDamage,
                    ProjectileLifespan = authoring.ProjectileLifespan,
                    ProjectileApplyGravity = authoring.ProjectileApplyGravity,
                    ProjectileRadius = authoring.ProjectileRadius
                });
            }
        }
    }
}