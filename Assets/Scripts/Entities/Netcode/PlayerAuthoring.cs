using Unity.Entities;
using UnityEngine;

internal class PlayerAuthoring : MonoBehaviour
{
}

internal class PlayerAuthoringBaker : Baker<PlayerAuthoring>
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