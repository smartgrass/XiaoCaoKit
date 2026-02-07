using MFPC;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using TEngine;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;

namespace XiaoCao
{
    public class MobileInputHud : GameStartMono, IClearCache
    {
        public Joystick joystick;

        public Transform slotParent;
        [ReadOnly] public List<SkillSlot> slots;

        public SkillSlot roleSkillBtn;
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
                    _atkTimer = GameDataCommon.LocalPlayer.component.atkTimer;
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

        public override void OnGameStart()
        {
            base.OnGameStart();
            slots = slotParent.GetComponentsInChildren<SkillSlot>().ToList();
            CheckBarImg();
            rollBtn.slotType = SlotType.Inputs;
            rollBtn.index = InputKey.LeftShift;

            norAtkBtn.onClick.AddListener(() => { PlayerInput.inputs[InputKey.NorAck] = true; });
            jumpBtn.onClick.AddListener(() => { PlayerInput.inputs[InputKey.Space] = true; });

            if (_tempRole == null)
            {
                roleSkillBtn.gameObject.SetActive(false);
            }

            GameEvent.AddEventListener<int>(EGameEvent.AddFriend.ToInt(), CheckRoleSkillBtn);
            UICanvasMgr.Inst.EventSystem.AddEventListener(UIEventNames.SkillChange,CheckBarImg);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            GameEvent.RemoveEventListener<int>(EGameEvent.AddFriend.ToInt(), CheckRoleSkillBtn);
            UICanvasMgr.Inst.EventSystem.RemoveEventListener(UIEventNames.SkillChange, CheckBarImg);
        }

        //当友方生成时触发
        void CheckRoleSkillBtn(int roleId)
        {
            AddRoleSkillBtn(roleId.GetRoleById());
        }

        private Role _tempRole;

        public void AddRoleSkillBtn(Role role)
        {
            roleSkillBtn.gameObject.SetActive(true);
            _tempRole = role;
            roleSkillBtn.slotType = SlotType.RoleSkill;
            roleSkillBtn.LoadSkillSprite();
        }

        private void Update()
        {
            if (GameAllData.CommonData.gameState != GameState.Running)
            {
                return;
            }

            CheckBtnInput();

            if (!BattleData.Current.CanPlayerControl || BattleData.Current.UIEnter)
            {
                return;
            }

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
            if (roleSkillBtn.isActiveAndEnabled)
            {
                roleSkillBtn.CheckFriendSkillUI();
            }
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