using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
            //角色输入与输入界面同步: MidCanvas(操作界面)不可用时清空输入, 不移动也不攻击
            if (!UIMgr.CanReadPlayerInput)
            {
                data.SetLocalXY(0f, 0f);
                data.ResetKey();
                data.norAckHold = false;
                return;
            }

            CheckInputXY();
            CheckNorAtk();

            if (Input.GetKeyDown(KeyCode.LeftShift))
                data.inputs[(int)InputKey.LeftShift] = true;

            if (Input.GetKeyDown(KeyCode.Mouse1))
                data.inputs[(int)InputKey.LeftShift] = true;

            if (Input.GetKeyDown(KeyCode.Space))
                data.inputs[(int)InputKey.Space] = true;

            if (Input.GetKeyDown(KeyCode.Tab))
                data.inputs[(int)InputKey.Tab] = true;

            if (Input.GetKeyDown(KeyCode.F))
                data.inputs[(int)InputKey.Focus] = true;

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
            bool keyHold = Input.GetKey(KeyCode.J);
            if (Input.GetKeyDown(KeyCode.J))
            {
                data.inputs[(int)InputKey.NorAck] = true;
            }

            if (GameSetting.UserInputType == UserInputType.Touch)
            {
                //Touch 模式: norAckHold 由普攻按钮的按压事件维护(KeyInputHud), 这里不覆盖
                return;
            }

            //按住鼠标左键且指针不在遮挡UI上; GetKey 在按下那一帧同样为 true, 与 GetKeyDown 共用一次判定
            bool mouseHold = Input.GetKey(KeyCode.Mouse0) && !IsPointerOverBlockingUI();
            if (mouseHold && Input.GetKeyDown(KeyCode.Mouse0))
            {
                data.inputs[(int)InputKey.NorAck] = true;
            }

            //长按普攻键/鼠标左键: 炮台自动连发(受攻击冷缩限制)
            data.norAckHold = keyHold || mouseHold;
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

                //伤害数字、敌人血条这类纯显示 UI 也会被射线命中, 它们正好压在第一人称准星(屏幕正中)上,
                //一旦被判定为"点在界面上", 按住开火就再也不响应。只有真能接收点击的控件才算遮挡
                if (IsInvisibleUI(hitObject) && !IsClickableUI(hitObject))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>命中项是否是完全透明的纯显示 UI(伤害数字淡出后仍是激活状态, 会继续参与 UI 射线)</summary>
        private static bool IsInvisibleUI(GameObject target)
        {
            Graphic graphic = target.GetComponent<Graphic>();
            return graphic != null && graphic.color.a <= 0.001f;
        }

        /// <summary>命中项是否是可点击控件(按钮/滑条/开关/带事件触发器的控件); 透明但可点击的 UI 仍然算遮挡</summary>
        private static bool IsClickableUI(GameObject target)
        {
            return target.GetComponentInParent<Selectable>() != null ||
                   target.GetComponentInParent<EventTrigger>() != null;
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
