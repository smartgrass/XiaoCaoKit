using UnityEngine;
using UnityEngine.UI;

namespace MFPC.Input.PlayerInput
{
    public class MobilePlayerInputHandler : PlayerInputHandler
    {
        [SerializeField] private Joystick _joystick;
        [SerializeField] private RunField _runField;
        [SerializeField] private TouchField _touchField;
        [SerializeField] private Button _jumpButton;

        #region MONO

        private void OnEnable()
        {
            _jumpButton.onClick.AddListener(SetJumpInput);
            _joystick.OnJoystickDragged += SetMoveInput;
        }

        private void OnDisable()
        {
            _jumpButton.onClick.RemoveAllListeners();
            _joystick.OnJoystickDragged -= SetMoveInput;
        }

        #endregion

        private void Update()
        {
            SetLookInput(_touchField.GetSwipeDirection);
            SetSprintInput(_runField.InRunField);
        }

        public void SetJoystickWithRunField(Joystick joystick, RunField runField)
        {
            _joystick.OnJoystickDragged -= SetMoveInput;

            _joystick = joystick;
            _runField = runField;

            _joystick.OnJoystickDragged += SetMoveInput;
        }
    }
}