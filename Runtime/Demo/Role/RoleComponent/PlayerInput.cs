using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Input = UnityEngine.Input;

namespace XiaoCao
{
    public class PlayerInput : PlayerComponent, IUsed
    {
        private static readonly List<RaycastResult> UiRaycastResults = new List<RaycastResult>();

        public PlayerInput(Player0 owner) : base(owner) { }

        public PlayerInputData data => Data_P.inputData;
        public RoleMovement movement => Data_P.movement;

        public override void Update()
        {
#if !UNITY_EDITOR
            if (Application.isMobilePlatform)
            {
                return;
            }
#endif
            CheckPcInputs();
        }

        private void CheckPcInputs()
        {
            CheckInputXY();

            // PC input check
            if (!BattleData.Current.CanPlayerControl || BattleData.Current.UIEnter)
            {
                return;
            }

            CheckNorAtk();

            if (Input.GetKeyDown(KeyCode.LeftShift))
                data.inputs[InputKey.LeftShift] = true;

            if (Input.GetKeyDown(KeyCode.Mouse1))
                data.inputs[InputKey.LeftShift] = true;

            if (Input.GetKeyDown(KeyCode.Space))
                data.inputs[InputKey.Space] = true;

            if (Input.GetKeyDown(KeyCode.Tab))
                data.inputs[InputKey.Tab] = true;

            if (Input.GetKeyDown(KeyCode.F))
                data.inputs[InputKey.Focus] = true;

            for (int index = 0; index < GameSetting.SkillCountOnBar; index++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + index))
                {
                    data.skillInput = index;
                }
            }

            for (int i = 0; i < data.CheckKeyCode2.Length; i++)
            {
                if (Input.GetKeyDown(data.CheckKeyCode2[i]))
                {
                    data.skillInput = i;
                }
            }
        }

        private void CheckNorAtk()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                data.inputs[InputKey.NorAck] = true;
            }

            if (GameSetting.UserInputType == UserInputType.Touch)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Mouse0) && !IsPointerOverBlockingUI())
            {
                data.inputs[InputKey.NorAck] = true;
            }
        }

        private static bool IsPointerOverBlockingUI()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            UiRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, UiRaycastResults);
            //EventSystem.current.IsPointerOverGameObject()
            for (int i = 0; i < UiRaycastResults.Count; i++)
            {
                GameObject hitObject = UiRaycastResults[i].gameObject;
                if (hitObject == null)
                {
                    continue;
                }

                if (IsTouchFieldObject(hitObject))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsTouchFieldObject(GameObject target)
        {
            Transform current = target != null ? target.transform : null;
            while (current != null)
            {
                if (current.CompareTag(Tags.TOUCH_FIELD))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void CheckInputXY()
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");
            data.SetLocalXY(x, y);
        }

        public override void FixedUpdate()
        {
            movement.SetMoveDir(GetInputMoveDir());
            data.ResetKey();
        }

        public Vector2 GetMouseProportionalCoordinates()
        {
            Vector3 mousePosition = Input.mousePosition;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            float proportionalX = mousePosition.x / screenWidth;
            float proportionalY = mousePosition.y / screenHeight;
            return new Vector2(proportionalX, proportionalY);
        }

        private Vector3 GetInputMoveDir()
        {
            Vector3 forward = movement.camForward;
            forward.y = 0;
            Vector3 hor = -Vector3.Cross(forward, Vector3.up).normalized;
            Vector3 moveDir = (data.Y * forward.normalized + data.X * hor).normalized;
            return moveDir;
        }

        public void Used()
        {
            data.ResetKey();
        }
    }
}
