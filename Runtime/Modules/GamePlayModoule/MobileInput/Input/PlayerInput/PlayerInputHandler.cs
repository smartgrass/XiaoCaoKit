using UnityEngine;
using System;
using MFPC.Input.SettingsInput;

namespace MFPC.Input.PlayerInput
{
    public abstract class PlayerInputHandler : MonoBehaviour, IPlayerInput
    {
        public float CalculatedHorizontalLookDirection => _lookDirection.x * _sensitiveData.Sensitivity * DeltaTimeMultiplier;
        public float CalculatedVerticalLookDirection => _lookDirection.y * _sensitiveData.Sensitivity * DeltaTimeMultiplier;
        public float LeanDirection { get; private set; }
        public bool IsSprint { get; private set; }
        public Vector2 MoveDirection { get; private set; }
        public event Action OnJumpAction;
        public event Action OnSitAction;

        protected virtual float DeltaTimeMultiplier => Time.deltaTime;           
        
        private Vector2 _lookDirection;
        private SensitiveData _sensitiveData;  
        
        public void Initialization(SensitiveData sensitiveData)
        {
            _sensitiveData = sensitiveData;
        }

        #region Input

        protected void SetJumpInput()
        {
            OnJumpAction?.Invoke();
        }

        protected void SetSitInput()
        {
            OnSitAction?.Invoke();
        }

        protected void SetLeanDirection(float leanDirection)
        {
            leanDirection = Mathf.Clamp(leanDirection, -1, 1);
            
            LeanDirection = leanDirection;
        }

        protected void SetMoveInput(Vector2 newMoveDirection)
        {
            MoveDirection = newMoveDirection;
        }

        protected void SetLookInput(Vector2 newLookDirection)
        {
            _lookDirection = newLookDirection;
        }

        protected void SetSprintInput(bool newSprintState)
        {
            IsSprint = newSprintState;
        }

        #endregion
    }
}