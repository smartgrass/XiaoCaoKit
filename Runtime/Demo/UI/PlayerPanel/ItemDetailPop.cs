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

        /// <summary>需要保持高度对齐的来源格子, 为 null 表示按预制体默认位置居中显示</summary>
        private ItemCell _alignToCell;

        /// <summary>当前显示的详情界面 RectTransform, 与 _alignToCell 配套决定对齐目标</summary>
        private RectTransform _alignRect;

        /// <summary>普通物品详情界面, 缓存以免每次取 transform</summary>
        private RectTransform _norRect;

        /// <summary>圣器详情界面, 缓存以免每次取 transform</summary>
        private RectTransform _relicRect;

        /// <summary>普通物品详情在预制体上的原始 Y</summary>
        private float _norDefaultY;

        /// <summary>圣器详情在预制体上的原始 Y</summary>
        private float _relicDefaultY;

        private void Awake()
        {
            hideBtn.onClick.AddListener(Hide);
            norItemDetailUI.backBtn.onClick.AddListener(Hide);

            //记下预制体上的原始 Y: 格子没开「详情维持相同高度」时用它还原, 不沿用上一次的对齐结果
            _norRect = norItemDetailUI.transform as RectTransform;
            _relicRect = relicItemDetailUI.transform as RectTransform;
            _norDefaultY = _norRect.localPosition.y;
            _relicDefaultY = _relicRect.localPosition.y;
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

            //格子上勾了「详情维持相同高度」时, 详情界面跟随该格子高度; 普通物品与圣器两套详情都支持
            _alignToCell = sourceCell != null && sourceCell.detailKeepSameHeight ? sourceCell : null;

            if (isRelic)
            {
                relicItemDetailUI.SetItem(item);
                _alignRect = _relicRect;
            }
            else
            {
                norItemDetailUI.SetItem(item);
                _alignRect = _norRect;
            }

            //SetItem 可能改高度(文本自适应/按格子数量算高), 位置调整放在其后
            if (_alignToCell != null)
            {
                AlignToSourceCell();
            }
            else
            {
                //没开「详情维持相同高度」: 还原预制体原始位置, 不能沿用上一次对齐后的 Y
                RestoreDetailPosition();
            }

            BindSourceCell(sourceCell);
        }

        /// <summary>
        /// 把当前详情界面垂直对齐到来源格子: 同属一个 Canvas, 换算到父节点坐标系后只改 Y。
        /// 结果会被夹在父节点范围内, 格子靠屏幕边缘时详情不会被顶出去。
        /// </summary>
        private void AlignToSourceCell()
        {
            if (_alignToCell == null || _alignRect == null)
            {
                return;
            }

            RectTransform detailRect = _alignRect;
            RectTransform parentRect = detailRect == null ? null : detailRect.parent as RectTransform;
            if (detailRect == null || parentRect == null)
            {
                return;
            }

            Vector3 cellLocal = parentRect.InverseTransformPoint(_alignToCell.transform.position);
            float halfDetail = detailRect.rect.height * 0.5f;
            float targetY = Mathf.Clamp(cellLocal.y,
                parentRect.rect.yMin + halfDetail,
                parentRect.rect.yMax - halfDetail);

            Vector3 localPos = detailRect.localPosition;
            if (Mathf.Abs(localPos.y - targetY) < 0.01f)
            {
                return;
            }

            localPos.y = targetY;
            detailRect.localPosition = localPos;
        }

        /// <summary>
        /// 把当前详情界面的 Y 还原成预制体原始值(两个面板都是 0), 用于没开「详情维持相同高度」的格子。
        /// </summary>
        private void RestoreDetailPosition()
        {
            if (_alignRect == null)
            {
                return;
            }

            float defaultY = _alignRect == _norRect ? _norDefaultY : _relicDefaultY;
            Vector3 localPos = _alignRect.localPosition;
            if (Mathf.Abs(localPos.y - defaultY) < 0.01f)
            {
                return;
            }

            localPos.y = defaultY;
            _alignRect.localPosition = localPos;
        }

        /// <summary>
        /// 关闭详情弹窗并清理当前显示内容。
        /// </summary>
        public void Hide()
        {
            BindSourceCell(null);
            _alignToCell = null;
            _alignRect = null;
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
