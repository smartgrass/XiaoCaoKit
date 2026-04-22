using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class SkillSlot : MonoBehaviour
    {
        public Image image;

        public Image fieldImg;

        public Image cdBlockImg;

        public GameObject rightTip;
        private TMP_Text _rightTipText; //在rightTip子物体中,目前用于显示道具数量
        
        public GameObject keyTip; //pc模式中,显示按键提示
        private TMP_Text _keyTipText; //在keyTip子物体中
        private string _defaultKeyTipText;

        public bool isReverField;

        public SimpleImageTween effectTween;

        public Color enableColor = Color.white;

        public Color disableColor = new Color(183 / 255f, 183 / 255f, 183 / 255f, 160 / 255f);

        #region RuntimeData

        public bool isColdLastFrame; //上一帧是否冷却中
        public bool skipNextClick;

        public int index;

        public SlotType slotType;

        #endregion
        private PlayerData0 _playerData;
        private GameObject _maskRoot;
        
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
                    _atkTimer = GameDataCommon.LocalPlayer.component.atkTimer;
                }
                return _atkTimer;
            }
        }

        private void Awake()
        {
            _maskRoot = image.transform.parent.gameObject;
            CacheKeyTipText();
            CacheRightTipText();
            RefreshRightTipUI();
        }

        private void Start()
        {
            AddBtnClickEvent();
            SetFinishState();
            RefreshInputTypeUI();
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
            SetFinishState();
        }

        private void SetFinishState()
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
            RefreshInputTypeUI();
            if (slotType == SlotType.RoleSkill)
            {
                int index = ConfigMgr.Inst.LocalRoleSetting.GetFriendRoleIndex(); 
                string roleKey = $"Role_{index}";
                image.sprite = SpriteResHelper.LoadRoleIcon(roleKey);
                return; 
            }

            if (slotType == SlotType.ExtraItem)
            {
                var extraItem = BattleData.Current.GetSelectedExtraItem();
                if (extraItem == null)
                {
                    image.enabled = false;
                    return;
                }

                image.enabled = true;
                image.sprite = extraItem.ToItem().GetItemSprite();
                RefreshRightTipUI();
                return;
            }


            string id = PlayerData.GetBarSkillId(index);
            if (string.IsNullOrEmpty(id))
            {
                image.enabled = false;
                return;
            }
            else
            {
                image.enabled = true;
            }
            
            image.sprite = SpriteResHelper.LoadSkillIcon(id);
            RefreshRightTipUI();
        }

        public void OnClickSkill()
        {
            if (skipNextClick)
            {
                skipNextClick = false;
                return;
            }

            if (!isColdLastFrame)
            {
                bool isSuccess = false;
                switch (slotType)
                {
                    case SlotType.SkillIndex:
                        playerInput.skillInput = index;
                        isSuccess = true;
                        break;
                    case SlotType.Inputs:
                        playerInput.inputs[index] = true;
                        isSuccess = true;
                        break;
                    case SlotType.RoleSkill:
                        isSuccess = GameDataCommon.LocalPlayer != null && GameDataCommon.LocalPlayer.PlayFriendRoleSKill();
                        break;
                    case SlotType.ExtraItem:
                        isSuccess = GameDataCommon.LocalPlayer != null && GameDataCommon.LocalPlayer.TryUseExtraSkill();
                        break;
                }

                if (isSuccess)
                {
                    PlayEffect();
                }
            }
        }

        public void CheckSlotUI(string skillId)
        {
            string id = PlayerData.GetBarSkillId(index);
            if (string.IsNullOrEmpty(id))
            {
                RefreshRightTipUI();
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            float process = AtkTimer.GetWaitTimeProccess(skillId);
            UpdateProcess(process);
        }
        
        public void CheckFriendSkillUI()
        {
            float process = PlayerData.GetFriendSkillProcess();
            UpdateProcess(process);
        }

        public void CheckExtraItemUI()
        {
            float process = BattleData.Current.GetSelectedExtraItemProcess();
            UpdateProcess(process);
            RefreshRightTipUI();
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

        private void CacheKeyTipText()
        {
            if (keyTip == null || _keyTipText != null)
            {
                return;
            }

            _keyTipText = keyTip.GetComponentInChildren<TMP_Text>(true);
            if (_keyTipText != null)
            {
                _defaultKeyTipText = _keyTipText.text;
            }
        }

        private void CacheRightTipText()
        {
            if (rightTip == null || _rightTipText != null)
            {
                return;
            }

            _rightTipText = rightTip.GetComponentInChildren<TMP_Text>(true);
        }

        private void EnsureRightTip()
        {
            if (rightTip != null)
            {
                CacheRightTipText();
                return;
            }

            GameObject rightTipGo = new GameObject("rightTip", typeof(RectTransform), typeof(Image));
            var rightTipRect = rightTipGo.GetComponent<RectTransform>();
            rightTipRect.SetParent(transform, false);
            rightTipRect.anchorMin = new Vector2(0.5f, 1f);
            rightTipRect.anchorMax = new Vector2(0.5f, 1f);
            rightTipRect.pivot = new Vector2(0.5f, 0.5f);
            rightTipRect.anchoredPosition = new Vector2(42f, -14f);
            rightTipRect.sizeDelta = new Vector2(50f, 50f);

            var rightTipImage = rightTipGo.GetComponent<Image>();
            rightTipImage.color = new Color(0f, 0f, 0f, 0.72f);
            rightTipImage.raycastTarget = false;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(rightTipRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = _keyTipText != null ? _keyTipText.font : TMP_Settings.defaultFontAsset;
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            rightTip = rightTipGo;
            _rightTipText = text;
        }

        private void RefreshRightTipUI()
        {
            bool isShow = false;
            string rightTipValue = string.Empty;

            if (slotType == SlotType.ExtraItem)
            {
                var extraItem = BattleData.Current.GetSelectedExtraItem();
                if (extraItem != null && !extraItem.isUnCount)
                {
                    isShow = true;
                    rightTipValue = extraItem.count.ToString();
                }
            }

            if (isShow && rightTip == null)
            {
                EnsureRightTip();
            }

            CacheRightTipText();
            if (rightTip != null)
            {
                rightTip.SetActive(isShow);
            }

            if (isShow && _rightTipText != null)
            {
                _rightTipText.text = rightTipValue;
            }
        }

        public void RefreshInputTypeUI()
        {
            CacheKeyTipText();
            RefreshRightTipUI();

            if (keyTip == null)
            {
                return;
            }

            string keyName = GetKeyTipName();
            bool isShow = GameSetting.UserInputType == UserInputType.Mouse;
            keyTip.SetActive(isShow);

            if (isShow && _keyTipText != null)
            {
                _keyTipText.text = string.IsNullOrEmpty(keyName) ? _defaultKeyTipText : keyName;
            }
        }

        private string GetKeyTipName()
        {
            switch (slotType)
            {
                case SlotType.SkillIndex:
                    return GetKeyCodeName(GetCheckKeyCode());
                case SlotType.Inputs:
                    return GetKeyCodeName(GetInputKeyCode());
                case SlotType.RoleSkill:
                    return GetKeyCodeName(GetCheckKeyCode());
                case SlotType.ExtraItem:
                    return GetKeyCodeName(KeyCode.Tab);
                default:
                    return string.Empty;
            }
        }

        private KeyCode GetInputKeyCode()
        {
            switch (index)
            {
                case InputKey.NorAck:
                    return KeyCode.Mouse0;
                case InputKey.LeftShift:
                    return KeyCode.LeftShift;
                case InputKey.Space:
                    return KeyCode.Space;
                case InputKey.Tab:
                    return KeyCode.Tab;
                case InputKey.Focus:
                    return KeyCode.F;
                default:
                    return KeyCode.None;
            }
        }

        private KeyCode GetCheckKeyCode()
        {
            var localPlayer = GameDataCommon.LocalPlayer;
            if (localPlayer == null || localPlayer.playerData == null)
            {
                return KeyCode.None;
            }

            var keyCodes = localPlayer.playerData.inputData.CheckKeyCode2;
            if (keyCodes == null || index < 0 || index >= keyCodes.Length)
            {
                return KeyCode.None;
            }

            return keyCodes[index];
        }

        private static string GetKeyCodeName(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.None:
                    return string.Empty;
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return "Shift";
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return "Ctrl";
                case KeyCode.Mouse0:
                    return "LMB";
                case KeyCode.Mouse1:
                    return "RMB";
            }

            string keyName = keyCode.ToString();
            if (keyName.StartsWith("Alpha"))
            {
                return keyName.Substring("Alpha".Length);
            }

            return keyName;
        }
    }

    public enum SlotType
    {
        SkillIndex,
        Inputs, //roll, jump
        RoleSkill,
        ExtraItem
    }
}
