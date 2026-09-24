using System.Collections.Generic;
using EasyUI.Toast;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCaoKit;
using XiaoCaoKit.UI;
using cfg;

namespace XiaoCao.UI
{
    /// <summary>
    /// 圣器锻造界面，负责按配置分页展示配方并执行材料消耗与锻造。
    /// </summary>
    public class ForgePanel : PanelBase
    {
        public override UIPanelType PanelType => UIPanelType.ForgePanel;

        [Header("界面")]
        public Transform slots;
        public Transform tabs;
        public Button backButton;

        private readonly List<int> _pages = new List<int>();
        private int _currentPage;
        private bool _isInitialized;

        private void Awake()
        {
            Init();
        }

        /// <summary>
        /// 显示锻造界面并刷新当前页的圣器配方。
        /// </summary>
        public override void Show(IUIData data = null)
        {
            gameObject.SetActive(true);
            Init();
            RefreshPages();
        }

        /// <summary>
        /// 隐藏圣器锻造界面。
        /// </summary>
        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 初始化配置、界面引用与按钮事件。
        /// </summary>
        public void Init()
        {
            if (_isInitialized)
            {
                return;
            }

            slots ??= transform.Find("Slots");
            tabs ??= transform.Find("TABS");

            if (backButton == null)
            {
                Transform backRoot = transform.Find("BackBtn");
                backButton = backRoot?.GetComponentInChildren<Button>(true);
            }

            backButton?.onClick.AddListener(OnClickBack);
            _isInitialized = true;
        }

        /// <summary>
        /// 根据配置生成页签，并打开第一个有效页面。
        /// </summary>
        private void RefreshPages()
        {
            _pages.Clear();
            List<ForgeRelicSetting> relicSettings = LubanTables.GetAllForgeRelicSettings();
            for (int i = 0; i < relicSettings.Count; i++)
            {
                int page = Mathf.Max(0, relicSettings[i].Page);
                if (!_pages.Contains(page))
                {
                    _pages.Add(page);
                }
            }

            _pages.Sort();
            if (_pages.Count == 0)
            {
                _currentPage = 0;
                RefreshPageTabs();
                RefreshRows();
                return;
            }

            if (!_pages.Contains(_currentPage))
            {
                _currentPage = _pages[0];
            }

            RefreshPageTabs();
            RefreshRows();
        }

        /// <summary>
        /// 刷新页签数量、文字、选中状态和点击事件。
        /// </summary>
        private void RefreshPageTabs()
        {
            if (tabs == null)
            {
                return;
            }

            UITool.SetCellListCount(tabs, _pages.Count);
            for (int i = 0; i < _pages.Count; i++)
            {
                int page = _pages[i];
                Transform tab = tabs.GetChild(i);
                Button button = tab.GetComponentInChildren<Button>(true);
                TMP_Text text = tab.GetComponentInChildren<TMP_Text>(true);
                UIStateChange stateChange = tab.GetComponent<UIStateChange>();

                if (text != null)
                {
                    text.text = $"第 {page + 1} 页";
                }

                if (stateChange != null)
                {
                    stateChange.SetState(page == _currentPage ? 1 : 0);
                }

                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => ShowPage(page));
                }
            }
        }

        /// <summary>
        /// 切换到指定配置页。
        /// </summary>
        private void ShowPage(int page)
        {
            _currentPage = page;
            RefreshPageTabs();
            RefreshRows();
        }

        /// <summary>
        /// 按配置行数刷新当前页；同一行的圣器会共同显示在对应的 ItemRoot 中。
        /// </summary>
        private void RefreshRows()
        {
            if (slots == null)
            {
                Debug.LogError("[ForgePanel] 未找到 Slots 节点。", this);
                return;
            }

            List<ForgeRelicSetting> rows = LubanTables.GetForgeRelicSettings(_currentPage);

            int maxRow = -1;
            Dictionary<int, List<ForgeRelicSetting>> rowMap =
                new Dictionary<int, List<ForgeRelicSetting>>();
            for (int i = 0; i < rows.Count; i++)
            {
                ForgeRelicSetting configRow = rows[i];
                int row = Mathf.Max(0, configRow.Row);
                if (!rowMap.TryGetValue(row, out List<ForgeRelicSetting> rowRelics))
                {
                    rowRelics = new List<ForgeRelicSetting>();
                    rowMap.Add(row, rowRelics);
                }

                rowRelics.Add(configRow);
                maxRow = Mathf.Max(maxRow, row);
            }

            UITool.SetCellListCount(slots, maxRow + 1);
            for (int row = 0; row <= maxRow; row++)
            {
                rowMap.TryGetValue(row, out List<ForgeRelicSetting> rowRelics);
                RefreshRelicLine(slots.GetChild(row), rowRelics);
            }
        }

        /// <summary>
        /// 根据同一行的圣器配置生成 ItemCell，并绑定各自的锁定状态与锻造事件。
        /// </summary>
        private void RefreshRelicLine(Transform line, List<ForgeRelicSetting> rowRelics)
        {
            Transform itemRoot = line.Find("ItemRoot");
            if (itemRoot == null)
            {
                Debug.LogError($"[ForgePanel] {line.name} 缺少 ItemRoot 节点。", line);
                return;
            }

            bool hasRelics = rowRelics != null && rowRelics.Count > 0;
            itemRoot.gameObject.SetActive(hasRelics);
            if (!hasRelics)
            {
                return;
            }

            UITool.SetCellListCount(itemRoot, rowRelics.Count);
            ItemCell[] cells = itemRoot.GetComponentsInChildren<ItemCell>(true);
            if (cells.Length == 0)
            {
                Debug.LogError($"[ForgePanel] {line.name} 缺少 ItemCell 模板。", line);
                return;
            }

            for (int i = 0; i < rowRelics.Count && i < cells.Length; i++)
            {
                ForgeRelicSetting configRow = rowRelics[i];
                ItemCell relicCell = cells[i];
                SetItemCell(relicCell, CreateRelicItem(configRow), UIItemTextType.NoNum);
                SetLockState(relicCell, !configRow.DefaultUnlocked);

                if (relicCell.btn != null)
                {
                    relicCell.btn.onClick.RemoveAllListeners();
                    relicCell.btn.interactable = configRow.DefaultUnlocked;
                    relicCell.btn.onClick.AddListener(() => TryForge(configRow));
                }
            }
        }

        /// <summary>
        /// 写入物品格数据和图标。
        /// </summary>
        private static void SetItemCell(ItemCell cell, Item item, UIItemTextType textType)
        {
            cell.SetItem(item, textType);
            if (cell.icon != null)
            {
                cell.icon.sprite = item.GetItemSprite();
                cell.icon.enabled = cell.icon.sprite != null;
            }
        }

        /// <summary>
        /// 设置物品格上的锁定遮罩。
        /// </summary>
        private static void SetLockState(ItemCell cell, bool isLocked)
        {
            Transform lockTransform = cell.transform.Find("lock");
            if (lockTransform == null)
            {
                lockTransform = cell.transform.Find("hasGet");
            }

            if (lockTransform != null)
            {
                lockTransform.gameObject.SetActive(isLocked);
            }
        }

        /// <summary>
        /// 将 Luban 圣器配置转换为背包物品。
        /// </summary>
        private static Item CreateRelicItem(ForgeRelicSetting configRow)
        {
            string qualityStr = configRow.Id.Split("_")[1];
            EQuality quality = int.TryParse(qualityStr, out int qualityInt) ? (EQuality)qualityInt : 0;
            return new Item(ItemType.HolyRelic, configRow.Id, 1, quality);
        }

        /// <summary>
        /// 检查并消耗材料，将锻造完成的圣器放入背包。
        /// </summary>
        private void TryForge(ForgeRelicSetting configRow)
        {
            if (!configRow.DefaultUnlocked)
            {
                Toast.Show("该圣器尚未解锁", 1);
                return;
            }

            Inventory inventory = PlayerSaveData.LocalSavaData?.inventory;
            if (inventory == null)
            {
                Debug.LogError("[ForgePanel] 玩家背包数据不存在。", this);
                return;
            }

            List<Item> materials = configRow.ForgeMaterials;
            if (!inventory.CheckEnoughItemList(materials, false))
            {
                Toast.Show("锻造材料不足", 1);
                return;
            }

            inventory.CheckEnoughItemList(materials, true);
            Item relicItem = CreateRelicItem(configRow);
            inventory.AddItem(relicItem);
            PlayerSaveData.SavaData();
            Toast.Show("圣器锻造成功", 1);
            RefreshRows();
        }

        private void OnClickBack()
        {
            UIMgr.Inst.HideView(PanelType);
        }
    }
}
