using Unity.Entities;

namespace Entities.Netcode.Shooting
{
    public struct TurretComponent : IComponentData
    {
        public float FireInterval;
        public float Timer;
        public bool IsInitialized;
        
        public float ProjectileSpeed;
        public float ProjectileDamage;
        public float ProjectileLifespan;
        public bool ProjectileApplyGravity;
        public float ProjectileRadius;
    }
}