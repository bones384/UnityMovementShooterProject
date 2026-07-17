using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Entities.Netcode
{
    public class PlayerStateComponentAuthoring : MonoBehaviour
    {
    }

    public class PlayerStateComponentAuthoringBaker : Baker<PlayerStateComponentAuthoring>
    {
        public override void Bake(PlayerStateComponentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerStateComponent
            {
                Health = 100f,
                LastKillerNetworkId = -1,
                LastDeathReason = -1,
                LastKillerTeamIndex = -1,
                CurrentAmmo = 30,
                HitMarkerTimer = 0f,
                WasLastHitFatal = false
            });
        }
    }

    public struct PlayerStateComponent : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        [GhostField] public float Health;
        [GhostField] public int TeamIndex;
        [GhostField] public float3 Velocity;
        [GhostField] public bool IsSliding;
        [GhostField] public bool IsWallRunning;
        [GhostField] public bool IsCrouching;
        [GhostField] public bool IsJumping;
        [GhostField] public bool IsParrying;
        [GhostField] public bool IsGrounded;
        [GhostField] public float CoyoteTimer;
        [GhostField] public float JumpBufferTimer;
        [GhostField] public float WallRunTimer;
        [GhostField] public float3 LastWallNormal;
        [GhostField] public bool PreviousPrimaryInput;
        [GhostField] public bool PreviousSecondaryInput;
        [GhostField] public bool PreviousThirdInput;
        [GhostField] public bool PreviousFourthInput;
        [GhostField] public float ParryTimer;

        [GhostField] public float PrimaryCooldownTimer;
        [GhostField] public float SecondaryCooldownTimer;
        [GhostField] public float ThirdCooldownTimer;
        [GhostField] public float FourthCooldownTimer;
        [GhostField] public int NetworkId;

        [GhostField] public bool IsDead;
        [GhostField] public float RespawnTimer;
        [GhostField] public float3 DeathPosition; // <-- NEW

        [GhostField] public int DeathCount; // Incremented once per death
        [GhostField] public int LastKillerNetworkId; // -1 if none/killbox
        [GhostField] public int LastDeathReason;
        [GhostField] public int LastKillerTeamIndex; // <-- NEW

        [GhostField] public int CurrentAmmo;

        // --- HIT FEEDBACK ---
        [GhostField] public float HitMarkerTimer;
        [GhostField] public bool WasLastHitFatal;
    }
}