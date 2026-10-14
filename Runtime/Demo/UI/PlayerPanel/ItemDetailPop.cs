using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    /// <summary>
    /// 物品详情弹窗，按物品类型切换普通物品与圣器两套详情界面。
    /// </summary>
    public class ItemDetailPop : MonoBehaviour
    {
        public NorItemDetailUI norItemDetailUI;
        public RelicItemDetailUI relicItemDetailUI;
        public Button hideBtn;

        /// <summary>
        /// 当前正在显示的物品。
        /// </summary>
        public Item CurrentItem { get; private set; }

        /// <summary>
        /// 弹出详情的来源格子，格子换物品或被隐藏时详情一并关闭。
        /// </summary>
        public ItemCell SourceCell { get; private set; }

        private void Awake()
        {
            hideBtn.onClick.AddListener(Hide);
            norItemDetailUI.backBtn.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            BindSourceCell(null);
            hideBtn.onClick.RemoveListener(Hide);
            norItemDetailUI.backBtn.onClick.RemoveListener(Hide);
        }

        /// <summary>
        /// 显示指定物品的详情：圣器走 RelicItemDetailUI，其余物品走 NorItemDetailUI。
        /// </summary>
        /// <param name="item">需要显示的物品。</param>
        /// <param name="sourceCell">弹出详情的来源格子，可为空。</param>
        public void Show(Item item, ItemCell sourceCell = null)
        {
            if (item == null)
            {
                return;
            }

            //先解绑旧来源，避免刷新内容时立刻触发失效回调
            BindSourceCell(null);
            CurrentItem = item;
            gameObject.SetActive(true);

            bool isRelic = item.type == ItemType.HolyRelic;
            norItemDetailUI.gameObject.SetActive(!isRelic);
            relicItemDetailUI.gameObject.SetActive(isRelic);

            if (isRelic)
            {
                relicItemDetailUI.SetItem(item);
            }
            else
            {
                norItemDetailUI.SetItem(item);
            }

            BindSourceCell(sourceCell);
        }

        /// <summary>
        /// 关闭详情弹窗并清理当前显示内容。
        /// </summary>
        public void Hide()
        {
            BindSourceCell(null);
            CurrentItem = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 绑定来源格子，同一时间只跟随一个格子；格子内容变化或被隐藏时详情自动关闭。
        /// </summary>
        private void BindSourceCell(ItemCell cell)
        {
            if (SourceCell != null)
            {
                SourceCell.OnInvalidated -= OnSourceCellInvalidated;
            }

            SourceCell = cell;
            if (SourceCell != null)
            {
                SourceCell.OnInvalidated += OnSourceCellInvalidated;
            }
        }

        /// <summary>
        /// 来源格子的内容已经变化，详情不再对应当前显示，直接关闭。
        /// </summary>
        private void OnSourceCellInvalidated(ItemCell cell)
        {
            Hide();
        }
    }
}
