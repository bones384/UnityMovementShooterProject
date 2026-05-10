using Unity.Entities;
using UnityEngine;

class PlayerAuthoring : MonoBehaviour
{
    
}

class PlayerAuthoringBaker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Player());
    }
}

public struct Player : IComponentData
{
    
}