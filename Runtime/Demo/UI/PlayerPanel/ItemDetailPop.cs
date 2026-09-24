using cfg;
using TMPro;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 物品详情弹窗，负责根据物品配置显示名称、描述和获取途径。
    /// </summary>
    public class ItemDetailPop : MonoBehaviour
    {
        public TMP_Text nameText;
        public TMP_Text descText;
        public TMP_Text getWayText;//获取途径
        
        public ItemCell itemCell;

        /// <summary>
        /// 当前正在显示的物品。
        /// </summary>
        public Item CurrentItem { get; private set; }

        /// <summary>
        /// 显示指定物品的详情。
        /// </summary>
        /// <param name="item">需要显示的物品。</param>
        public void Show(Item item)
        {
            gameObject.SetActive(true);
            SetItem(item);
        }

        /// <summary>
        /// 根据物品 id 查询 ItemInfo 配置并刷新详情内容。
        /// </summary>
        /// <param name="item">需要显示的物品。</param>
        public void SetItem(Item item)
        {
            CurrentItem = item;
            if (item == null)
            {
                Clear();
                return;
            }

            ItemInfo itemInfo = LubanTables.GetItemInfo(item.typeId);
            if (itemInfo == null)
            {
                Debug.LogWarning($"物品详情缺少 ItemInfo 配置：{item.typeId}");
                SetText(item.typeId, string.Empty, string.Empty);
            }
            else
            {
                SetText(itemInfo.Name, itemInfo.DescCN, itemInfo.ObtainWayCN);
            }

            RefreshItemCell(item);
        }

        /// <summary>
        /// 隐藏详情弹窗并清理当前显示内容。
        /// </summary>
        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 清理详情文本和可选的物品格显示。
        /// </summary>
        private void Clear()
        {
            CurrentItem = null;
            SetText(string.Empty, string.Empty, string.Empty);

            if (itemCell != null)
            {
                itemCell.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 同步详情中的物品格；物品格未绑定时仅显示文字信息。
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

        /// <summary>
        /// 写入详情弹窗中的三个文本字段。
        /// </summary>
        private void SetText(string itemName, string description, string obtainWay)
        {
            if (nameText != null)
            {
                nameText.text = itemName ?? string.Empty;
            }

            if (descText != null)
            {
                descText.text = description ?? string.Empty;
            }

            if (getWayText != null)
            {
                getWayText.text = obtainWay ?? string.Empty;
            }
        }
    }
}
