using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Entities.Netcode
{
    public struct PlayerState
    {
        public float3 Position;
        public quaternion Rotation;
        public float3 Velocity;
        public bool IsSliding;
        public bool IsWallRunning;
        public bool IsCrouching;
        public bool IsJumping;
        public bool IsParrying;
    }

    public class PlayerBridge : MonoBehaviour
    {
        public PlayerState PlayerState;
        public static PlayerBridge Instance;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {   
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

        }

        void Start()
        {
            PlayerState = new PlayerState();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}