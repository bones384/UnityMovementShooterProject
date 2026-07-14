using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

internal class PlayerLookAuthoring : MonoBehaviour
{
    public float3 CameraOffset = new(0, 1.8f, 0);
}

internal class PlayerLookAuthoringBaker : Baker<PlayerLookAuthoring>
{
    public override void Bake(PlayerLookAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PlayerLook
        {
            CameraOffset = authoring.CameraOffset
        });
    }
}

public struct PlayerLook : IInputComponentData
{
    public float Pitch;
    public float Yaw;

    public float3 CameraOffset;
}