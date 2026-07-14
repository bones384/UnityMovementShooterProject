using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Entities.Netcode
{
    public struct HitscanBulletRequest : IComponentData
    {
        public Entity Owner; // Changed to Entity for easier component lookups
        public float Damage;
        public float Range;
        public float3 Origin;
        public float3 Direction;
        
        public NetworkTick Tick; 
    }
}