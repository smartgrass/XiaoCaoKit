using System;
using EasyUI.Toast;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;

namespace XiaoCao.UI
{
    /// <summary>
    /// 单个属性祝福面板，负责显示等级、效果、碎片进度和升级按钮。
    /// </summary>
    public class AttributeBlessingUI : MonoBehaviour
    {
        public TMP_Text title;
        public TMP_Text levelText;
        public TMP_Text effectText;
        public TMP_Text costText;
        public Image img; //类型图标
        public Button upGradeBtn;
        public Slider itemCountSlider; //显示消耗和已有
        public SpriteSettingSo blessingIconSetting;

        private EBlessing _blessing;
        private Action<EBlessing> _onUpgrade;
        private bool _hasValue;

        private PlayerSaveData PlayerSaveData => GameAllData.playerSaveData;

        private void Awake()
        {
            if (upGradeBtn != null)
            {
                upGradeBtn.onClick.AddListener(OnUpgradeBtnClick);
            }
        }

        private void OnDestroy()
        {
            if (upGradeBtn != null)
            {
                upGradeBtn.onClick.RemoveListener(OnUpgradeBtnClick);
            }
        }

        /// <summary>
        /// 设置当前面板绑定的祝福类型并刷新显示。
        /// </summary>
        public void SetValue(EBlessing blessing, Action<EBlessing> onUpgrade)
        {
            _blessing = blessing;
            _onUpgrade = onUpgrade;
            _hasValue = true;
            Refresh();
        }

        /// <summary>
        /// 刷新当前祝福等级、效果、碎片进度和按钮状态。
        /// </summary>
        public void Refresh()
        {
            if (!_hasValue)
            {
                return;
            }

            PlayerSaveData playerSaveData = PlayerSaveData;
            if (playerSaveData == null)
            {
                SetInteractable(false);
                return;
            }

            int level = playerSaveData.GetBlessingLevel(_blessing);
            bool isMaxLevel = BlessingRule.IsMaxLevel(level);
            int cost = playerSaveData.GetBlessingNextCost(_blessing);
            int hasCount = playerSaveData.GetBlessingFragmentCount(_blessing);

            RefreshTitle(level);
            RefreshEffect(level, isMaxLevel, hasCount, cost);
            RefreshSlider(isMaxLevel, hasCount, cost);
            SetInteractable(!isMaxLevel && hasCount >= cost);

            img.sprite = blessingIconSetting.values[(int)_blessing];
        }

        private void RefreshTitle(int level)
        {
            string blessingName = BlessingRule.GetName(_blessing);
            if (title != null)
            {
                title.text = levelText == null
                    ? $"{blessingName} {LocalizeKey.Lv.ToLocalizeStr()}{level}/{BlessingRule.MaxLevel}"
                    : blessingName;
            }

            if (levelText != null)
            {
                levelText.text = $"{LocalizeKey.Lv.ToLocalizeStr()}{level}/{BlessingRule.MaxLevel}";
            }
        }

        private void RefreshEffect(int level, bool isMaxLevel, int hasCount, int cost)
        {
            string effect = BlessingRule.GetLevelEffectText(_blessing, level);
            string costLine = isMaxLevel
                ? LocalizeKey.BlessingFullLevel.ToLocalizeStr()
                : LocalizeKey.FormatWithArgs(
                    LocalizeKey.BlessingCostLine,
                    BlessingRule.GetFragmentName(_blessing),
                    GetCostCountText(hasCount, cost));

            if (effectText != null)
            {
                effectText.text = $"{effect}";
            }

            if (costText != null)
            {
                costText.text = costLine;
            }
        }

        private void RefreshSlider(bool isMaxLevel, int hasCount, int cost)
        {
            if (itemCountSlider == null)
            {
                return;
            }

            itemCountSlider.wholeNumbers = true;
            if (isMaxLevel)
            {
                itemCountSlider.minValue = 0;
                itemCountSlider.maxValue = 1;
                itemCountSlider.value = 1;
                return;
            }

            itemCountSlider.minValue = 0;
            itemCountSlider.maxValue = Mathf.Max(1, cost);
            itemCountSlider.value = Mathf.Clamp(hasCount, 0, cost);
        }

        private string GetCostCountText(int hasCount, int cost)
        {
            if (hasCount < cost)
            {
                return $"<color=red>{hasCount}</color>/{cost}";
            }

            return $"{hasCount}/{cost}";
        }

        private void SetInteractable(bool interactable)
        {
            if (upGradeBtn != null)
            {
                upGradeBtn.interactable = interactable;
            }
        }

        private void OnUpgradeBtnClick()
        {
            PlayerSaveData playerSaveData = PlayerSaveData;
            if (playerSaveData == null)
            {
                return;
            }

            bool isSuccess = playerSaveData.TryUpgradeBlessing(_blessing);
            if (!isSuccess)
            {
                Toast.Show(LocalizeKey.NoEnough.ToLocalizeStr(), 1);
                Refresh();
                return;
            }

            Toast.Show(LocalizeKey.LevelUp.ToLocalizeStr());
            Refresh();
            _onUpgrade?.Invoke(_blessing);
        }
    }
}
