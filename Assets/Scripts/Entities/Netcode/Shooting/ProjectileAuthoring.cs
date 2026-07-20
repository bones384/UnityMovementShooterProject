using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode.Shooting
{
    public class ProjectileAuthoring : MonoBehaviour
    {
        private class Baker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new ProjectileComponent
                {
                    Lifespan = 5f,
                    Radius = 0.2f
                });
            }
        }
    }
}