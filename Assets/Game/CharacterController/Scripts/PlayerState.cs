using UnityEngine;
namespace Game.CharacterController
{
    public class PlayerState : MonoBehaviour
    {
        [field: SerializeField] public PlayerMovementState CurrentPlayerMovementState { get; private set; } = PlayerMovementState.Idle;
        public void SetPlayerMovementState(PlayerMovementState playerMovementState)
        {
            CurrentPlayerMovementState = playerMovementState;
        }
        public bool InGroundedState()
        {
            return IsStateGroundedState(CurrentPlayerMovementState);
        }
        public bool IsStateGroundedState(PlayerMovementState movementState)
        {
            return movementState == PlayerMovementState.Idle ||
                movementState == PlayerMovementState.Running ||
                movementState == PlayerMovementState.Sliding;
        }
    }
    public enum PlayerMovementState
    {
        Idle = 0,
        Running = 1,
        Sliding = 2,
        Dashing = 3,
        Jumping = 4,
        Falling = 5,
        Strafing = 6
    }
}

