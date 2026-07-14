using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Entities.Netcode.Shooting
{
    // The active, moving projectile
    [GhostComponent(PrefabType = GhostPrefabType.All)]
    public struct ProjectileComponent : IComponentData
    {
        [GhostField] public Entity Owner;
        [GhostField] public float3 Velocity;
        [GhostField] public float Damage;
        [GhostField] public float Lifespan;
        [GhostField] public bool ApplyGravity;
        [GhostField] public float Radius;
    }

    // The one-frame request to spawn a projectile
    public struct ProjectileSpawnRequest : IComponentData
    {
        public Entity Owner;
        public float3 Origin;
        public float3 Direction;
        public float Speed;
        public float Damage;
        public float Lifespan;
        public bool ApplyGravity;
        public float Radius;
    }
}