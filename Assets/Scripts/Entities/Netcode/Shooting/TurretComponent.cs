using Unity.Entities;

namespace Entities.Netcode.Shooting
{
    public struct TurretComponent : IComponentData
    {
        public float FireInterval; 
        public float Timer; // Back to a simple float!
        public bool IsInitialized;
        
        // Projectile Parameters
        public float ProjectileSpeed;
        public float ProjectileDamage;
        public float ProjectileLifespan;
        public bool ProjectileApplyGravity;
        public float ProjectileRadius;
    }
}