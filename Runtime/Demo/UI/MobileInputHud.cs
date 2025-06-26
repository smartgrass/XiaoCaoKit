using MFPC;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class MobileInputHud : GameStartMono, IClearCache
    {
        public Joystick joystick;

        public Transform slotParent;
        [ReadOnly]
        public List<SkillSlot> slots;

        public SkillSlot rollBtn;
        public Button jumpBtn;
        public Button norAtkBtn;

        public PlayerInputData PlayerInput => PlayerData.inputData;

        public PlayerSetting PlayerSetting => PlayerData.playerSetting;

        private PlayerAtkTimer _atkTimer;
        public PlayerAtkTimer AtkTimer
        {
            get
            {
                if (_atkTimer == null)
                {
                    _atkTimer = GameDataCommon.LocalPlayer.component.atkTimers;
                }
                return _atkTimer;
            }
        }

        public PlayerData0 _playerData;
        public PlayerData0 PlayerData
        {
            get
            {
                if (_playerData == null)
                {
                    _playerData = GameDataCommon.LocalPlayer.playerData;
                }
                return _playerData;
            }
        }




        public void Show()
        {
            gameObject.SetActive(true);
        }
        public override void OnGameStart()
        {
            base.OnGameStart();
            slots = slotParent.GetComponentsInChildren<SkillSlot>().ToList();
            CheckBarImg();
            rollBtn.slotType = SlotType.Inputs;
            rollBtn.index = InputKey.LeftShift;

            norAtkBtn.onClick.AddListener(() =>
            {
                PlayerInput.inputs[InputKey.NorAck] = true;
            });
            jumpBtn.onClick.AddListener(() =>
            {
                PlayerInput.inputs[InputKey.Space] = true;
            });

        }

        private void Update()
        {
            if (GameAllData.commonData.gameState != GameState.Running)
            {
                return;
            }
            if (!BattleData.Current.CanPlayerControl || BattleData.Current.UIEnter)
            {
                return;
            }
            CheckBtnInput();

            CheckUIUpdate();
        }

        private void CheckUIUpdate()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var solt = slots[i];
                solt.CheckSlotUI(PlayerData.GetBarSkillId(i));
            }
            rollBtn.CheckSlotUI(PlayerSetting.rollSkillId);
        }


        private void CheckBtnInput()
        {
            //checkInput
            Vector2 input = joystick.GetInputV;
            PlayerInput.SetXY(input.x, input.y);
        }

        private void CheckBarImg()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var solt = slots[i];
                solt.index = i;
                solt.LoadSkillSprite();
            }
        }
    }
}


