using Unity.Entities;
using Unity.Physics;

namespace Entities.Netcode.Shooting
{
    public struct IgnoreOwnerColliderCollector : ICollector<ColliderCastHit>
    {
        public Entity IgnoreEntity;
        
        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; private set; }
        public int NumHits { get; private set; }
        public ColliderCastHit ClosestHit;

        public IgnoreOwnerColliderCollector(Entity ignoreEntity)
        {
            IgnoreEntity = ignoreEntity;
            MaxFraction = 1f;
            NumHits = 0;
            ClosestHit = default;
        }

        public bool AddHit(ColliderCastHit hit)
        {
            if (hit.Entity == IgnoreEntity)
                return false;

            MaxFraction = hit.Fraction;
            ClosestHit = hit;
            NumHits = 1;
            return true;
        }
    }
}