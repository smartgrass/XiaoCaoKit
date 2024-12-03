using System;
using UnityEngine;

namespace MFPC.Input.PlayerInput
{
    public class ProxyPlayerInputHandler : IPlayerInput
    {
        public float CalculatedHorizontalLookDirection { get => _isLockInput ? 0.0f : _playerInputHandler.CalculatedHorizontalLookDirection ; }
        public float CalculatedVerticalLookDirection { get => _isLockInput ? 0.0f : _playerInputHandler.CalculatedVerticalLookDirection ; }
        public float LeanDirection { get => _isLockInput ? 0.0f : _playerInputHandler.LeanDirection; }
        public bool IsSprint { get => !_isLockInput && _playerInputHandler.IsSprint; }
        public Vector2 MoveDirection { get => _isLockInput ? Vector2.zero : _playerInputHandler.MoveDirection; }
        public event Action OnJumpAction;
        public event Action OnSitAction;
        
        private SensitiveData _sensitiveData;
        private PlayerInputHandler _playerInputHandler;
        private bool _isLockInput;

        public ProxyPlayerInputHandler(SensitiveData sensitiveData)
        {
            _sensitiveData = sensitiveData;
        }

        public void SetPlayerInputHandler(PlayerInputHandler playerInputHandler)
        {
            if (playerInputHandler == null) return;

            UnsubscribeActions();
            
            _playerInputHandler = playerInputHandler;
            _playerInputHandler.Initialization(_sensitiveData);
            
            if(!_isLockInput) SubscribeActions();
        }

        public void SetLockInput(bool isLockInput)
        {
            _isLockInput = isLockInput;
            
            if(isLockInput) UnsubscribeActions();
            else SubscribeActions();
        }
        
        private void SubscribeActions()
        {
            _playerInputHandler.OnJumpAction += JumpAction;
            _playerInputHandler.OnSitAction += SitAction;
        }

        private void UnsubscribeActions()
        {
            if(_playerInputHandler == null) return;
            
            _playerInputHandler.OnJumpAction -= JumpAction;
            _playerInputHandler.OnSitAction -= SitAction;
        }

        public void JumpAction() => OnJumpAction?.Invoke();
        public void SitAction() => OnSitAction?.Invoke();
    }
}