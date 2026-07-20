using Unity.Entities;

namespace Entities.Netcode.GameMechanics
{
    public struct SpawnPointComponent : IComponentData
    {
        public int TeamIndex;
    }
}