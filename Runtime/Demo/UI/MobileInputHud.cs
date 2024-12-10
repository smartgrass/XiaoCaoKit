using MFPC;
using UnityEngine;

namespace XiaoCao
{
    public class MobileInputHud: MonoBehaviour
    {
        public Joystick joystick;

        public PlayerInputData playerInput;


        private void Update()
        {
            if (!BattleData.Current.CanPlayerControl || BattleData.Current.UIEnter)
            {
                return;
            }
            if (playerInput == null)
            {
                playerInput = GameDataCommon.LocalPlayer.playerData.inputData;
            }
            Vector2 input = joystick.GetInputV;
            playerInput.AddInputXY(input.x, input.y);
        }
    }
}


