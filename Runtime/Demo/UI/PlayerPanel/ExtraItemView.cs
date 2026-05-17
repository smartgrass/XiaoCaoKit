using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    /// <summary>
    /// 展示当前关卡可用道具，并在点击道具时显示详情。
    /// </summary>
    public class ExtraItemView : EnableShowUI
    {
        public Transform itemContainer;

        public TextMeshProUGUI itemTitle;

        public TextMeshProUGUI itemDesc;

        // public TextMeshProUGUI itemCountText;
        // public TextMeshProUGUI itemCdText;
        public Image itemIcon;

        public int baseCount = 10;

        private readonly List<ExtraItemCell> _cellList = new List<ExtraItemCell>();
        private List<BattleExtraItemData> _extraItems = new List<BattleExtraItemData>();
        private BattleExtraItemData _selectedItem;

        public override void OnEnable()
        {
            UpdateUI();
        }

        private void Update()
        {
            if (_selectedItem != null)
            {
                RefreshDetail(_selectedItem);
            }
        }

        public override void UpdateUI()
        {
            _extraItems = BattleData.Current != null
                ? BattleData.Current.GetExtraItems()
                : new List<BattleExtraItemData>();

            int hasCount = _extraItems.Count;
            int showCount = Math.Max(baseCount, hasCount);

            _cellList.Clear();
            UITool.SetCellListCount(itemContainer, showCount);
            _cellList.AddRange(itemContainer.GetComponentsInChildren<ExtraItemCell>(false));

            if (_cellList.Count < showCount)
            {
                Debug.LogError($"道具容器配置错误，ExtraItemCell数量不足：{_cellList.Count}/{showCount}");
                return;
            }

            for (int i = 0; i < showCount; i++)
            {
                var cell = _cellList[i];
                cell.Index = i;

                if (i < hasCount)
                {
                    SetItemCellInfo(cell, _extraItems[i]);
                }
                else
                {
                    SetItemCellInfo(cell, null);
                }

                cell.gameObject.SetActive(true);
            }

            if (hasCount > 0)
            {
                OnItemClick(_extraItems[Mathf.Clamp(BattleData.Current.selectedExtraItemIndex, 0, hasCount - 1)]);
            }
            else
            {
                ClearDetail();
            }
        }

        /// <summary>
        /// 点击道具时刷新右侧或下方详情区域。
        /// </summary>
        public void OnItemClick(BattleExtraItemData item)
        {
            if (item == null)
            {
                ClearDetail();
                return;
            }

            _selectedItem = item;
            RefreshDetail(item);
        }

        private void SetItemCellInfo(ExtraItemCell itemCell, BattleExtraItemData item)
        {
            itemCell.SetValue(item);

            itemCell.onButtonClick = null;
            itemCell.onButtonClick += OnItemClick;
        }

        private void RefreshDetail(BattleExtraItemData item)
        {
            BattleExtraItemSubConfig config = ConfigMgr.Inst.BattleExtraItemConfigSo?.GetConfig(item.typeId);
            itemTitle.text = item.typeId.ToLocalizeStr();

            string mainDesc = LocalizeKey.GetExtraItemDescKey(item.typeId).ToLocalizeStr();

            string cdStr = GetCdText(item);

            itemDesc.text = $"{mainDesc}{cdStr}";
            if (itemIcon != null)
            {
                itemIcon.sprite = item.ToItem().GetItemSprite();
                itemIcon.enabled = true;
            }
        }

        private void ClearDetail()
        {
            _selectedItem = null;

            if (itemTitle != null)
            {
                itemTitle.text = "";
            }

            if (itemDesc != null)
            {
                itemDesc.text = "";
            }

            if (itemIcon != null)
            {
                itemIcon.enabled = false;
            }
        }
        

        private string GetCdText(BattleExtraItemData item)
        {
            if (item.baseCd > 0)
            {
                string numStr = item.baseCd.ToString("0.#");
                return $"\n{"CD".ToLocalizeStr()}:{numStr}s";
            }

            return "";
        }
    }
}