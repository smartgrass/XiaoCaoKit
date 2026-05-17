using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 关卡道具列表中的单个道具格子。
    /// </summary>
    public class ExtraItemCell : MonoBehaviour, IPointerClickHandler
    {
        public Image bg;
        public Image icon;
        public GameObject countTextBg;
        public TextMeshProUGUI countText;
        public Button btn;

        [XCHeader("DebugView")] public BattleExtraItemData extraItem;

        public int Index { get; set; }
        public Action<BattleExtraItemData> onButtonClick;

        private void OnEnable()
        {
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnButtonClick);
                btn.onClick.AddListener(OnButtonClick);
            }
        }

        private void OnDisable()
        {
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnButtonClick);
            }
        }

        /// <summary>
        /// 设置道具数据并刷新显示。
        /// </summary>
        public void SetValue(BattleExtraItemData item)
        {
            extraItem = item;
            RefreshView();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (btn == null)
            {
                OnButtonClick();
            }
        }

        /// <summary>
        /// 刷新道具格子的图标、数量和冷却显示。
        /// </summary>
        public void RefreshView()
        {
            bool hasItem = extraItem != null && !string.IsNullOrEmpty(extraItem.typeId);
            SetImageEnable(icon, hasItem);

            if (hasItem && icon != null)
            {
                icon.enabled = true;
                icon.sprite = extraItem.ToItem().GetItemSprite();
            }
            else
            {
                icon.enabled = false;  
            }

            //当个数小于2时不显示 数量
            countTextBg.gameObject.SetActive(hasItem && extraItem.count > 1);

            countText.text = hasItem && !extraItem.isUnCount ? extraItem.count.ToString() : "";
        }

        private void OnButtonClick()
        {
            onButtonClick?.Invoke(extraItem);
        }

        private void SetImageEnable(Image image, bool isEnable)
        {
            if (image != null)
            {
                image.enabled = isEnable;
            }
        }
    }
}