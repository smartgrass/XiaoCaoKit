using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;
using XiaoCaoKit;

namespace XiaoCao
{
    /// <summary>
    /// 圣器升级窗口，负责显示升级前后信息和处理升级面板按钮。
    /// </summary>
    public class RelicOnEquipDetailUpgradeUI : MonoBehaviour
    {
        public TMP_Text nameText;
        public TMP_Text qualityText;

        public Transform attrLineContainer;
        public UpgradeAttrLine[] attrLines;
        public ItemCell itemCell;
        public Button confirmUpgradeBtn;
        public Button backBtn;
        public Button hideBtn;
        public ControlUISizeByCellCount controlUISize;

        private Action _confirmUpgradeAction;
        private Action _backAction;
        private Action _hideAction;

        private void Awake()
        {
            confirmUpgradeBtn?.onClick.AddListener(OnConfirmUpgradeClick);
            backBtn?.onClick.AddListener(OnBackClick);
            hideBtn?.onClick.AddListener(OnHideClick);
        }

        private void OnDestroy()
        {
            confirmUpgradeBtn?.onClick.RemoveListener(OnConfirmUpgradeClick);
            backBtn?.onClick.RemoveListener(OnBackClick);
            hideBtn?.onClick.RemoveListener(OnHideClick);
        }

        /// <summary>
        /// 设置升级窗口的按钮回调。
        /// </summary>
        public void SetActions(Action confirmUpgradeAction, Action backAction, Action hideAction)
        {
            _confirmUpgradeAction = confirmUpgradeAction;
            _backAction = backAction;
            _hideAction = hideAction;
        }

        /// <summary>
        /// 显示升级窗口并刷新升级预览。
        /// </summary>
        public void Show(Item item)
        {
            if (item == null)
            {
                return;
            }

            gameObject.SetActive(true);
            Refresh(item);
        }

        /// <summary>
        /// 刷新升级前后的标题、物品格和属性行。
        /// </summary>
        public void Refresh(Item item)
        {
            if (item == null)
            {
                return;
            }

            RelicItem currentRelic = item.ToRelicItem();
            Item previewItem = RelicDetailHelper.CreateUpgradePreviewItem(item);
            RelicItem previewRelic = previewItem.ToRelicItem();

            if (nameText != null)
            {
                nameText.text =
                    $"{currentRelic.LocalizeName} Lv{currentRelic.level} -> Lv{previewRelic.level}";
                nameText.color = currentRelic.GetColor();
            }

            if (qualityText != null)
            {
                qualityText.text = $"{"QualityName".ToLocalizeStr()}: {(item.quality).ToLocalizedName()}";
            }

            RefreshItemCell(previewItem);
            RefreshAttrLines(item, previewItem);
            controlUISize?.UpdateSize();
        }

        /// <summary>
        /// 隐藏升级窗口。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnConfirmUpgradeClick()
        {
            _confirmUpgradeAction?.Invoke();
        }

        private void OnBackClick()
        {
            _backAction?.Invoke();
        }

        private void OnHideClick()
        {
            _hideAction?.Invoke();
        }

        /// <summary>
        /// 刷新升级窗口中的预览物品格。
        /// </summary>
        private void RefreshItemCell(Item item)
        {
            if (itemCell == null)
            {
                return;
            }

            itemCell.gameObject.SetActive(true);
            itemCell.SetItem(item, UIItemTextType.NoNum);
        }

        /// <summary>
        /// 刷新升级窗口中的属性对比行。
        /// </summary>
        private void RefreshAttrLines(Item currentItem, Item previewItem)
        {
            if (attrLineContainer == null)
            {
                return;
            }

            Dictionary<EAttr, float> currentValues = RelicValueCalc.GetRelicValue(currentItem);
            Dictionary<EAttr, float> previewValues = RelicValueCalc.GetRelicValue(previewItem);
            List<EAttr> attrs = currentValues.Keys.ToList();

            UITool.SetCellListCount(attrLineContainer, attrs.Count);
            if (attrLines == null)
            {
                return;
            }

            for (int i = 0; i < attrs.Count && i < attrLines.Length; i++)
            {
                EAttr attr = attrs[i];
                previewValues.TryGetValue(attr, out float afterValue);
                attrLines[i]?.Show(
                    attr,
                    currentValues[attr],
                    afterValue);
            }
        }
    }

    /// <summary>
    /// 圣器详情界面的共享数据处理工具。
    /// </summary>
    internal static class RelicDetailHelper
    {
        /// <summary>
        /// 创建升级后的临时物品，用于显示升级预览。
        /// </summary>
        public static Item CreateUpgradePreviewItem(Item item)
        {
            Item previewItem = new Item(item.type, item.typeId, item.num, item.quality);
            RelicItem relicItem = item.ToRelicItem();
            SetRelicLevel(previewItem, relicItem.level + 1);
            return previewItem;
        }

        /// <summary>
        /// 同步圣器的 typeId 等级段和数量等级字段。
        /// </summary>
        public static void SetRelicLevel(Item item, int level)
        {
            if (item == null || string.IsNullOrEmpty(item.typeId))
            {
                return;
            }

            string[] parts = item.typeId.Split('_');
            if (parts.Length >= 3)
            {
                parts[2] = level.ToString();
                item.typeId = string.Join("_", parts);
            }

            item.num = level;
        }
    }

    /// <summary>
    /// 圣器升级属性行，负责显示属性名、升级前数值和升级后数值。
    /// </summary>
    public class UpgradeAttrLine : MonoBehaviour
    {
        public TMP_Text attrNameText;
        public TMP_Text beforeNumText;
        public TMP_Text afterNumText;

        /// <summary>
        /// 刷新单行升级属性变化显示。
        /// </summary>
        public void Show(EAttr attr, float beforeValue, float afterValue)
        {
            gameObject.SetActive(true);

            if (attrNameText != null)
            {
                attrNameText.text = attr.ToLocalizedName();
            }

            if (beforeNumText != null)
            {
                beforeNumText.text = attr.GetEAttrNumString(beforeValue);
            }

            if (afterNumText != null)
            {
                afterNumText.text = attr.GetEAttrNumString(afterValue);
            }
        }
    }
}
