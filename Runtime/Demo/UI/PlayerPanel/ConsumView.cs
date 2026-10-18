using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    /// <summary>
    /// 背包页签下的消耗品列表: 左侧显示背包中的消耗品格子, 点击后在右侧详情区显示图标、名称、数量与描述。
    /// </summary>
    /// <see cref="BagTabView"/>
    public class ConsumView : EnableShowUI
    {
        public Transform itemContainer;
        public Image itemIcon;
        public TextMeshProUGUI itemName;
        public TextMeshProUGUI itemCount;
        public TextMeshProUGUI itemDesc;

        private readonly List<ItemCell> _cellList = new List<ItemCell>();
        private readonly List<Item> _consumList = new List<Item>();

        public override void OnEnable()
        {
            UpdateUI();
        }

        /// <summary>
        /// 按背包中的消耗品刷新格子, 并按顺序选中第一个物品显示详情。
        /// </summary>
        public override void UpdateUI()
        {
            CollectConsumItems();

            int count = _consumList.Count;
            UITool.SetCellListCount(itemContainer, count);

            _cellList.Clear();
            _cellList.AddRange(itemContainer.GetComponentsInChildren<ItemCell>(true));

            if (_cellList.Count < count)
            {
                Debug.LogError($"消耗品容器配置错误，ItemCell数量不足：{_cellList.Count}/{count}");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                SetCellInfo(_cellList[i], _consumList[i]);
            }

            if (count > 0)
            {
                RefreshDetail(_consumList[0]);
            }
            else
            {
                ClearDetail();
            }
        }

        /// <summary>
        /// 从背包中筛出数量大于 0 的消耗品, 保持背包原有顺序。
        /// </summary>
        private void CollectConsumItems()
        {
            _consumList.Clear();

            List<Item> items = PlayerSaveData.LocalSavaData.inventory.items;
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                if (item.type == ItemType.Consumable && item.num > 0)
                {
                    _consumList.Add(item);
                }
            }
        }

        /// <summary>
        /// 绑定格子的物品与点击回调, 点击只刷新右侧详情, 不弹物品详情窗。
        /// </summary>
        private void SetCellInfo(ItemCell itemCell, Item item)
        {
            itemCell.SetItem(item);
            itemCell.clickMode = EItemClickMode.Custom;
            itemCell.clickAction = cell => RefreshDetail(cell.Item);
        }

        /// <summary>
        /// 在右侧详情区显示消耗品的图标、名称、持有数量与描述。
        /// </summary>
        private void RefreshDetail(Item item)
        {
            itemIcon.sprite = item.GetItemSprite();
            itemIcon.enabled = itemIcon.sprite != null;

            itemName.text = item.GetItemName();
            itemName.color = item.quality.GetQualityColor();

            itemCount.text = $"{LocalizeKey.Owned.ToLocalizeStr()}: {item.num}";
            itemDesc.text = item.GetItemDesc();
        }

        /// <summary>
        /// 背包里没有消耗品时清空右侧详情, 并给出空背包提示。
        /// </summary>
        private void ClearDetail()
        {
            itemIcon.enabled = false;
            itemName.text = string.Empty;
            itemCount.text = string.Empty;
            itemDesc.text = LocalizeKey.BagEmpty.ToLocalizeStr();
        }
    }
}
