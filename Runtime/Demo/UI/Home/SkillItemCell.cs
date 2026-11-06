using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XiaoCao.UI;

namespace XiaoCao.UI
{
    /// <see cref="ActiveSkillView"/>
    public class SkillItemCell : MonoBehaviour, IPointerClickHandler
    {
        public Image bg;
        public Image icon;
        public GameObject lockMask;
        public GameObject equipObj;

        //技能可学习/升级的红点
        public GameObject BtnRedDot;

        public TMP_Text titleText;
        public TMP_Text lvText;

        public string skillId;
        public bool IsUnlock { get; set; }

        [OnValueChanged(nameof(ShowType))] public ESkillItemCellType cellType;


        public Action clickAct;


        #region control

        public void ShowSkillUI(string skillId, ESkillItemCellType cellType)
        {
            this.skillId = skillId;
            this.cellType = cellType;
            UpdateUI();
        }

        public void UpdateUI()
        {
            icon.enabled = !string.IsNullOrEmpty(skillId);
            icon.sprite = SpriteResHelper.LoadSkillIcon(skillId);
            titleText.text = LocalizeKey.GetSkillNameKey(skillId).ToLocalizeStr();
            ShowType();
            RefreshRedDot();
        }

        /// <summary>
        /// 刷新学习红点: 只有技能总览格子才提示, 已装备栏的小格子不提示
        /// </summary>
        public void RefreshRedDot()
        {
            //未绑红点节点时不显示也不报错
            if (BtnRedDot == null)
            {
                return;
            }

            BtnRedDot.SetActive(cellType == ESkillItemCellType.SkillViewAll && SkillLearnRedDot.IsNew(skillId));
        }

        private void ShowType()
        {
            if (cellType == ESkillItemCellType.SkillViewEquip)
            {
                titleText.gameObject.SetActive(false);
                lvText.gameObject.SetActive(false);
                lockMask.gameObject.SetActive(false);
            }
            else if (cellType == ESkillItemCellType.SkillViewAll)
            {
                titleText.gameObject.SetActive(true);
                lvText.gameObject.SetActive(true);
                int level = PlayerHelper.GetSkillLevel(skillId);
                IsUnlock = level > 0;
                lockMask.gameObject.SetActive(!IsUnlock);
                lvText.text = $"{PlayerHelper.GetSkillLevel(skillId)}/{PlayerHelper.GetSkillMaxLevel(skillId)}";
            }
        }

        #endregion


        public void OnPointerClick(PointerEventData eventData)
        {
            //点过一次就当作已查看, 红点不再提示该等级
            if (cellType == ESkillItemCellType.SkillViewAll)
            {
                SkillLearnRedDot.MarkRead(skillId);
                RefreshRedDot();
            }

            clickAct?.Invoke();
        }

        public void SetEquipState(bool equip)
        {
            if (cellType == ESkillItemCellType.SkillViewAll)
            {
                equipObj.SetActive(equip);
            }
        }
    }


    public enum ESkillItemCellType
    {
        SkillViewAll,
        SkillViewEquip,
    }
}