using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// --- 1. THE ECS EVENT REQUEST ---
public enum SFX 
{ 
    FireHitscan, FireProj, HitGround, HitConfirm, 
    Parry, Death, TakeDamage, KothStart, KothCap, Footstep, Reload, Victory, Loss
}

public struct AudioRequest : IComponentData
{
    public SFX Effect;
    public float3 Position;
    public float Pitch;
    public int LocalTargetNetworkId; // -1 = play for everyone, else only plays for this specific player
}

// --- 2. THE UNITY MANAGER ---
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Player")]
    public AudioClip footstep;
    public AudioClip takeDamage;
    public AudioClip death;

    [Header("Weapons")]
    public AudioClip fireHitscan;
    public AudioClip fireProjectile;
    public AudioClip hitGround;
    public AudioClip hitConfirm;
    public AudioClip parry;
    public AudioClip reload; // <-- NEW
    [Header("Game Over")]
    public AudioClip victory; // <-- NEW
    public AudioClip defeat;  // <-- NEW
    [Header("Objective")]
    public AudioClip kothCapStart;
    public AudioClip kothCaptured;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Play3D(SFX sfx, Vector3 position, float pitch = 1f)
    {
        AudioClip clip = GetClip(sfx);
        if (clip == null) return;

        GameObject go = new GameObject($"SFX_{sfx}");
        go.transform.position = position;
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.pitch = pitch;
        source.spatialBlend = 1f; // 3D Sound
        source.Play();
        Destroy(go, clip.length + 0.1f);
    }

    public void Play2D(SFX sfx, float pitch = 1f)
    {
        AudioClip clip = GetClip(sfx);
        if (clip != null)
        {
            // Simple 2D playback for hitmarkers and local damage
            GameObject go = new GameObject($"SFX_2D_{sfx}");
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.pitch = pitch;
            source.spatialBlend = 0f; // 2D Sound
            source.Play();
            Destroy(go, clip.length + 0.1f);
        }
    }

    private AudioClip GetClip(SFX sfx) => sfx switch
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