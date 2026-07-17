using Unity.Entities;
using UnityEngine;

namespace Entities.Netcode
{
    public class EntitiesReferenceAuthoring : MonoBehaviour
    {
        public GameObject playerPrefab;

        public float maxSpeed = 10f;
        public float initialSpeed = 6f;
        public float gravity = 19.6f;
        public float acceleration = 60f;
        public float jumpSpeed = 8.0f;
        public float dampenSpeed = 25.0f;
        public float maxFallSpeed = 35f;
        public float airControlFactor = 0.2f;
        public float WallRunMaxTime = 1.0f; // Time before slowdown starts
        public float WallRunDrag = 15f; // How fast they slow down
        public float WallRunMinSpeed = 4f; // Drop-off threshold
        public float WallJumpBoost = 1.2f; // Multiplier when jumping off a wall
        public bool ApplyWallGravity = true;
        public float WallGravityMultiplier = 0.4f; // 40% of normal gravity
        public GameObject ProjectilePrefab;
        public float PrimaryCooldown;
        public float SecondaryCooldown;
        public float ThirdCooldown;
        public float FourthCooldown;
        public float RespawnDelay = 5f;
        public int MagazineSize = 30;
        public float ReloadTime = 5f;
        public float FireRate = 0.2f;
    }

    internal class RotationSpeedBaker : Baker<EntitiesReferenceAuthoring>
    {
        public override void Bake(EntitiesReferenceAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntitiesReferences
            {
                PlayerPrefabEntity = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic),
                MaxSpeed = authoring.maxSpeed,
                Acceleration = authoring.acceleration,
                JumpSpeed = authoring.jumpSpeed,
                InitialSpeed = authoring.initialSpeed,
                Gravity = authoring.gravity,
                DampenSpeed = authoring.dampenSpeed,
                MaxFallSpeed = authoring.maxFallSpeed,
                AirControlFactor = authoring.airControlFactor,
                WallRunMaxTime = authoring.WallRunMaxTime,
                WallRunDrag = authoring.WallRunDrag,
                WallRunMinSpeed = authoring.WallRunMinSpeed,
                WallJumpBoost = authoring.WallJumpBoost,
                ApplyWallGravity = authoring.ApplyWallGravity,
                WallGravityMultiplier = authoring.WallGravityMultiplier,
                ProjectilePrefab = GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic),
                primaryCooldown = authoring.PrimaryCooldown,
                secondaryCooldown = authoring.SecondaryCooldown,
                thirdCooldown = authoring.ThirdCooldown,
                fourthCooldown = authoring.FourthCooldown,
                RespawnDelay = authoring.RespawnDelay,
                MagazineSize = authoring.MagazineSize,
                ReloadTime = authoring.ReloadTime,
                FireRate = authoring.FireRate
            });
        }
    }

    public struct EntitiesReferences : IComponentData
    {
        public Entity PlayerPrefabEntity;
        public float MaxSpeed;
        public float Acceleration;
        public float JumpSpeed;
        public float InitialSpeed;
        public float Gravity;
        public float DampenSpeed;
        public float MaxFallSpeed;
        public float AirControlFactor;
        public float WallRunMaxTime;
        public float WallRunDrag;
        public float WallRunMinSpeed;
        public float WallJumpBoost;
        public bool ApplyWallGravity;
        public float WallGravityMultiplier;
        public Entity ProjectilePrefab;
        public float primaryCooldown;
        public float secondaryCooldown;
        public float thirdCooldown;
        public float fourthCooldown;
        public float RespawnDelay;
        public int MagazineSize;
        public float ReloadTime;
        public float FireRate;
    }
}