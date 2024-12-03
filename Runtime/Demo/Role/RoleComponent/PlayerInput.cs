using UnityEngine;
using Input = UnityEngine.Input;

namespace XiaoCao
{
    public class PlayerInput : PlayerComponent, IUsed
    {
        public PlayerInput(Player0 owner) : base(owner) { }

        public PlayerInputData data => Data_P.inputData;
        public RoleMovement movement => Data_P.movement;

        public override void Update()
        {
            if (!BattleData.Current.CanPlayerControl || BattleData.Current.UIEnter)
            {
                return;
            }

            CheckInputXY();

            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.J))
            {
                data.inputs[InputKey.NorAck] = true;
                ////屏幕安全边距
                //if (GetMouseProportionalCoordinates().y < 0.9f)
                //{

                //}

            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
                data.inputs[InputKey.LeftShift] = true;

            if (Input.GetKeyDown(KeyCode.Space))
                data.inputs[InputKey.Space] = true;

            if (Input.GetKeyDown(KeyCode.Tab))
                data.inputs[InputKey.Tab] = true;

            if (Input.GetKeyDown(KeyCode.F))
                data.inputs[InputKey.Focus] = true;

            for (int i = 0; i < 6; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    data.skillInput = i;
                }
            }

            for (int i = 0; i < data.CheckKeyCode2.Length; i++)
            {
                if (Input.GetKeyDown(data.CheckKeyCode2[i]))
                {
                    data.skillInput = i;
                }
            }

            owner.playerData.inputData.Copy(data);
        }

        private void CheckInputXY()
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");
            data.AddInputXY(x, y);
        }

        public override void FixedUpdate()
        {
            movement.SetMoveDir(GetInputMoveDir());
        }

        public Vector2 GetMouseProportionalCoordinates()
        {
            Vector3 mousePosition = Input.mousePosition;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // 将屏幕坐标转换为比例坐标  
            float proportionalX = mousePosition.x / screenWidth;
            float proportionalY = mousePosition.y / screenHeight;
            // 返回比例坐标  
            return new Vector2(proportionalX, proportionalY);

        }

        private Vector3 GetInputMoveDir()
        {
            Vector3 forward = movement.camForward;
            forward.y = 0;
            Vector3 hor = -Vector3.Cross(forward, Vector3.up).normalized;
            Vector3 moveDir = (data.y * forward.normalized + data.x * hor).normalized;
            return moveDir;
        }

        //使用过
        public void Used()
        {
            data.Reset();
        }
    }

}
