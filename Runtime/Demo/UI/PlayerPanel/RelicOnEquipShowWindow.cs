using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;

namespace XiaoCao
{
    /// <summary>
    /// 圣器普通详情窗口，负责显示基础信息和处理窗口按钮。
    /// </summary>
    public class RelicOnEquipShowWindow : MonoBehaviour
    {
        public TMP_Text nameText;
        public TMP_Text qualityText;

        public Transform attrTextContainer;
        public Transform attrNumTextContainer;
        public ItemCell itemCell;

        public Button unEquipBtn;
        public Button upgradeBtn;
        public Button backBtn;
        public Button hideBtn;
        public ControlUISizeByCellCount controlUISize;

        private AttrNameNumGroupUI _attrNameNumGroupUI;
        private Action _unEquipAction;
        private Action _upgradeAction;
        private Action _closeAction;

        private void Awake()
        {
            _attrNameNumGroupUI = new AttrNameNumGroupUI(attrTextContainer, attrNumTextContainer);
            unEquipBtn?.onClick.AddListener(OnUnEquipClick);
            upgradeBtn?.onClick.AddListener(OnUpgradeClick);
            backBtn?.onClick.AddListener(OnCloseClick);
            hideBtn?.onClick.AddListener(OnCloseClick);
        }

        private void OnDestroy()
        {
            unEquipBtn?.onClick.RemoveListener(OnUnEquipClick);
            upgradeBtn?.onClick.RemoveListener(OnUpgradeClick);
            backBtn?.onClick.RemoveListener(OnCloseClick);
            hideBtn?.onClick.RemoveListener(OnCloseClick);
        }

        /// <summary>
        /// 设置普通窗口的按钮回调。
        /// </summary>
        public void SetActions(Action unEquipAction, Action upgradeAction, Action closeAction)
        {
            _unEquipAction = unEquipAction;
            _upgradeAction = upgradeAction;
            _closeAction = closeAction;
        }

        /// <summary>
        /// 刷新普通窗口中的圣器信息。
        /// </summary>
        public void Refresh(Item item)
        {
            if (item == null)
            {
                return;
            }

            RelicItem relicItem = item.ToRelicItem();
            if (nameText != null)
            {
                nameText.text = RelicDetailHelper.GetRelicTitle(relicItem);
                nameText.color = relicItem.GetColor();
            }

            if (qualityText != null)
            {
                qualityText.text = $"{"QualityName".ToLocalizeStr()}: {(item.quality).ToLocalizedName()}";
            }

            Dictionary<EAttr, float> valueDic = RelicValueCalc.GetRelicValue(item);
            _attrNameNumGroupUI?.ShowAttrUI(valueDic.Keys.ToList(), valueDic.Values.ToList());
            RefreshItemCell(item);
        }

        /// <summary>
        /// 显示普通窗口并更新窗口高度。
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            controlUISize?.UpdateSize();
        }

        /// <summary>
        /// 隐藏普通窗口。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnUnEquipClick()
        {
            _unEquipAction?.Invoke();
        }

        private void OnUpgradeClick()
        {
            _upgradeAction?.Invoke();
        }

        private void OnCloseClick()
        {
            _closeAction?.Invoke();
        }

        /// <summary>
        /// 刷新普通窗口中的物品格。
        /// </summary>
        private void RefreshItemCell(Item item)
        {
            if (itemCell == null)
            {
                return;
            }

            itemCell.gameObject.SetActive(true);
            itemCell.SetItem(item, UIItemTextType.NoNum);
            if (itemCell.icon == null)
            {
                return;
            }

            itemCell.icon.sprite = item.GetItemSprite();
            itemCell.icon.enabled = itemCell.icon.sprite != null;
        }
    }
}
