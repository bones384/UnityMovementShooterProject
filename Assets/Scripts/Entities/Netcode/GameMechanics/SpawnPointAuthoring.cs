using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode.GameMechanics
{
    public class SpawnPointAuthoring : MonoBehaviour
    {
        public int TeamIndex;

        class Baker : Baker<SpawnPointAuthoring>
        {
            public override void Bake(SpawnPointAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpawnPointComponent
                {
                    TeamIndex = authoring.TeamIndex
                });
            }
        }
    }
}