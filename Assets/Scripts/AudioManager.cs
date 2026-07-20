using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum SFX
{
    FireHitscan,
    FireProj,
    HitGround,
    HitConfirm,
    Parry,
    Death,
    TakeDamage,
    KothStart,
    KothCap,
    Footstep,
    Reload,
    Victory,
    Loss
}

public struct AudioRequest : IComponentData
{
    public SFX Effect;
    public float3 Position;
    public float Pitch;
    public int LocalTargetNetworkId;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Player")] public AudioClip footstep;

    public AudioClip takeDamage;
    public AudioClip death;

    [Header("Weapons")] public AudioClip fireHitscan;

    public AudioClip fireProjectile;
    public AudioClip hitGround;
    public AudioClip hitConfirm;
    public AudioClip parry;
    public AudioClip reload;

    [Header("Game Over")] public AudioClip victory;

    public AudioClip defeat;

    [Header("Objective")] public AudioClip kothCapStart;

    public AudioClip kothCaptured;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Play3D(SFX sfx, Vector3 position, float pitch = 1f)
    {
        var clip = GetClip(sfx);
        if (clip == null) return;

        var go = new GameObject($"SFX_{sfx}");
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.pitch = pitch;
        source.spatialBlend = 1f;
        source.Play();
        Destroy(go, clip.length + 0.1f);
    }

    public void Play2D(SFX sfx, float pitch = 1f)
    {
        var clip = GetClip(sfx);
        if (clip != null)
        {
            var go = new GameObject($"SFX_2D_{sfx}");
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.pitch = pitch;
            source.spatialBlend = 0f;
            source.Play();
            Destroy(go, clip.length + 0.1f);
        }
    }

    private AudioClip GetClip(SFX sfx)
    {
        return sfx switch
        {
            SFX.FireHitscan => fireHitscan,
            SFX.FireProj => fireProjectile,
            SFX.HitGround => hitGround,
            SFX.HitConfirm => hitConfirm,
            SFX.Parry => parry,
            SFX.Death => death,
            SFX.TakeDamage => takeDamage,
            SFX.KothStart => kothCapStart,
            SFX.KothCap => kothCaptured,
            SFX.Footstep => footstep,
            SFX.Reload => reload,
            SFX.Victory => victory,
            SFX.Loss => defeat,
            _ => null
        };
    }
}