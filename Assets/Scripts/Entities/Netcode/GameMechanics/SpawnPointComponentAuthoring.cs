using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode.GameMechanics
{
    public struct SpawnPointComponent : IComponentData
    {
        public int TeamIndex; // 0 for Team A, 1 for Team B
    }
    
}