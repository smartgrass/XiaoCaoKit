using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XiaoCao
{
    public class SkillItemCell : MonoBehaviour, IPointerClickHandler
    {
        public Image bg;
        public Image icon;
        public GameObject lockMask;

        public string skillId;
        public bool IsUnlock { get; set; }


        public Action clickAct;


        #region control
        public void UpdateUI()
        {
            icon.sprite = SpriteResHelper.LoadSkillIcon(skillId);
            lockMask.gameObject.SetActive(!IsUnlock);
        }

        #endregion


        public void OnPointerClick(PointerEventData eventData)
        {
            clickAct?.Invoke();
        }
    }
}