using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XiaoCao.UI;

namespace XiaoCao.UI
{
    /// <see cref="SkillView"/>
    public class SkillItemCell : MonoBehaviour, IPointerClickHandler
    {
        public Image bg;
        public Image icon;
        public GameObject lockMask;
        public GameObject equipObj;

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