using UnityEngine;
using MFPC.Utils;

namespace MFPC
{
    [System.Flags]
    public enum PlayerStates
    {
        Move = 1,
        Jump = 2,
        Sit = 4,
        Run = 8,
        Ladder = 16,
        Lean = 32
    }

    [CreateAssetMenu(fileName = "PlayerData", menuName = "MFPC/PlayerData")]
    public class PlayerData : ScriptableObject
    {
        [field: SerializeField] public PlayerStates AvailablePlayerStates { get; private set; } = PlayerStates.Move;

        #region Move State
        
        [HeaderData("Move")]
        [field: SerializeField] public float WalkSpeed { get; private set; }
        [field: SerializeField] public float Gravity { get; private set; }
        [field: SerializeField] public bool AirControl { get; private set; }

        #endregion
        
        #region Run State

        [HeaderData("Run")]
        [field: SerializeField]  public float RunSpeed { get; private set; }
        
        #endregion
        
        #region Jump State
        
        [HeaderData("Jump")]
        [field: SerializeField] public float JumpForce { get; private set; }
        [field: SerializeField] public AudioClip JumpSFX { get; private set; }
        [field: SerializeField] public float UnderRayDistance { get; private set; } = 0.1f;
        
        #endregion

        /// <summary>
        /// Checks whether such a condition exists
        /// </summary>
        public bool IsAvailableState(PlayerStates state)
        {
            return AvailablePlayerStates.HasFlag(state);
        }
    }
}