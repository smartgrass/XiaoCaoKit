using UnityEngine;

namespace MFPC.Input.PlayerInput
{
    public class OldPlayerInputHandler : PlayerInputHandler
    {
#if !ENABLE_INPUT_SYSTEM
        private Vector2 previousMoveInput;

        protected override float DeltaTimeMultiplier => 1.0f;

        private void Awake()
        {
            previousMoveInput = Vector2.zero;
        }

        public void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) SetJumpInput();
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftControl)) SetSitInput();
            
            SprintInput();
            MoveInput();
            LookInput();
            LeanInput();
        }

        private void SprintInput()
        {
            if (UnityEngine.Input.GetKey(KeyCode.LeftShift)) SetSprintInput(true);
            if (UnityEngine.Input.GetKeyUp(KeyCode.LeftShift)) SetSprintInput(false);
        }

        private void MoveInput()
        {
            float horizontalInput = UnityEngine.Input.GetAxis("Horizontal");
            float verticalInput = UnityEngine.Input.GetAxis("Vertical");

            Vector2 moveInput = new Vector2(horizontalInput, verticalInput);

            if (previousMoveInput != Vector2.zero && moveInput == Vector2.zero)
            {
                SetMoveInput(Vector2.zero);
            }

            previousMoveInput = moveInput;

            if (moveInput != Vector2.zero)
            {
                SetMoveInput(moveInput);
            }
        }

        private void LookInput()
        {
            float horizontalLookInput = UnityEngine.Input.GetAxis("Mouse X");
            float verticalLookInput = UnityEngine.Input.GetAxis("Mouse Y");

            Vector2 lookInput = new Vector2(horizontalLookInput, -verticalLookInput);

            SetLookInput(lookInput);
        }

        private void LeanInput()
        {
            float direction = 0f;

            if (UnityEngine.Input.GetKey(KeyCode.Q)) direction = -1;
            else if (UnityEngine.Input.GetKey(KeyCode.E)) direction = 1;

            SetLeanDirection(direction);
        }
#endif
    }
}