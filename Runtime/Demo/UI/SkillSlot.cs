using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class SkillSlot : MonoBehaviour
    {
        public Image image;

        public Image fieldImg;

        public Image cdBlockImg;

        public bool isReverField;

        public SimpleImageTween effectTween;

        public Color enableColor = Color.white;

        public Color disableColor = new Color(183 / 255f, 183 / 255f, 183 / 255f, 160 / 255f);

        #region RuntimeData

        public bool isColdLastFrame; //上一帧是否冷却中

        public int index;

        public SlotType slotType;

        #endregion
        private PlayerData0 _playerData;
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

        public PlayerInputData playerInput => PlayerData.inputData;


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


        private void Start()
        {
            AddBtnClickEvent();
            SetFinshState();
        }

        private void AddBtnClickEvent()
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnClickSkill);
            }
        }

        public void OnUpdate(float fill)
        {
            if (isReverField)
            {
                fill = 1 - fill;
            }
            fieldImg.fillAmount = fill;
            image.color = Color.Lerp(disableColor, enableColor, fill);
        }

        public void Clear()
        {

        }

        internal void CdFinish()
        {
            PlayEffect();
            SetFinshState();
        }

        private void SetFinshState()
        {
            if (cdBlockImg)
            {
                cdBlockImg.enabled = false;
            }
            image.color = enableColor;
        }

        private void PlayEffect()
        {
            effectTween.Play();
        }

        internal void CdEnter()
        {
            if (cdBlockImg)
            {
                cdBlockImg.enabled = true;
            }
        }

        public void LoadSkillSprite()
        {
            if (slotType == SlotType.RoleSkill)
            {
                int index = ConfigMgr.LocalRoleSetting.GetFriendRoleIndex(); 
                string roleKey = $"Role_{index}";
                image.sprite = SpriteResHelper.LoadRoleIcon(roleKey);
                return; 
            }
            
            
            string id = PlayerData.GetBarSkillId(index);
            image.sprite = SpriteResHelper.LoadSkillIcon(id);
        }

        public void OnClickSkill()
        {
            if (!isColdLastFrame)
            {
                switch (slotType)
                {
                    case SlotType.SkillIndex:
                        playerInput.skillInput = index;
                        break;
                    case SlotType.Inputs:
                        playerInput.inputs[index] = true;
                        break;
                    case SlotType.RoleSkill:
                        GameDataCommon.LocalPlayer.PlayFriendRoleSKill();
                        break;
                }

                PlayEffect();
            }
        }

        public void CheckSlotUI(string skillId)
        {
            float process = AtkTimer.GetWaitTimeProccess(skillId);

            UpdateProcess(process);
        }
        
        public void CheckFriendSkillUI()
        {
            float process = PlayerData.GetFriendSkillProcess();
            UpdateProcess(process);
        }


        private void UpdateProcess(float process)
        {
            bool isCd = process != 0;
            if (isColdLastFrame && !isCd)
            {
                //播放发光特效
                CdFinish();
            }
            if (!isColdLastFrame && isCd)
            {
                CdEnter();
            }

            isColdLastFrame = isCd;

            //更新进度
            OnUpdate(process);
        }
    }

    public enum SlotType
    {
        SkillIndex,
        Inputs, //roll, jump
        RoleSkill
    }
}
