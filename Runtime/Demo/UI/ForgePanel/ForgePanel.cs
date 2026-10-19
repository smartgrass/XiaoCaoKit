using System.Collections.Generic;
using EasyUI.Toast;
using TEngine;
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

        [Header("界面")] public Transform slots;
        public Transform tabs;
        public Button backBtn;

        [Header("锻造详情")] public TMP_Text forgeItemNameText;
        public TMP_Text qualityText;
        public ItemCell forgeItemCell;
        public Transform materialSlots;
        public Transform attrLineContainer;
        public Button confirmForgeBtn;

        public GameObject lockObject;
        private GameObject _confirmForgeBtnRoot;
        public UIStateChange selectStateChange;
        private UIStateChange _confirmForgeBtnStateChange;

        private readonly List<int> _pages = new List<int>();
        private int _currentPage;
        private ForgeRelicSetting _selectedForgeSetting;
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
            ClearSelect();
            gameObject.SetActive(false);
        }

        public void ClearSelect()
        {
            _selectedForgeSetting = null;
            RefreshForgeDetail();
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

            backBtn.onClick.AddListener(OnClickBack);
            confirmForgeBtn.onClick.AddListener(OnClickConfirmForge);
            //状态组件可能挂在按钮自身, 也可能挂在父节点上, 两处都找一遍
            _confirmForgeBtnStateChange = confirmForgeBtn.GetComponent<UIStateChange>() ??
                                         confirmForgeBtn.transform.parent.GetComponent<UIStateChange>();
            _confirmForgeBtnRoot = _confirmForgeBtnStateChange != null
                ? _confirmForgeBtnStateChange.gameObject
                : confirmForgeBtn.transform.parent.gameObject;
            _isInitialized = true;
            ClearSelect();
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
                    text.text = $"Lv {page + 1}";
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
                relicCell.SetItem(RelicItem.CreateItem(configRow.Id), UIItemTextType.NoNum);
                SetLockState(relicCell, !configRow.IsRelicUnlock());

                relicCell.btn.onClick.RemoveAllListeners();
                relicCell.btn.onClick.AddListener(() => ShowForgeDetail(configRow));
            }
        }

        /// <summary>
        /// 设置物品格上的锁定遮罩。
        /// </summary>
        private static void SetLockState(ItemCell cell, bool isLocked)
        {
            Transform lockTransform = cell.transform.Find("lock");
            if (lockTransform)
            {
                lockTransform.gameObject.SetActive(isLocked);
            }
        }

        /// <summary>
        /// 打开锻造详情，并展示当前圣器的产出物与所需材料。
        /// </summary>
        private void ShowForgeDetail(ForgeRelicSetting configRow)
        {
            _selectedForgeSetting = configRow;
            RefreshForgeDetail();
        }

        /// <summary>
        /// 刷新锻造详情中的产出物、材料数量和制作按钮状态。
        /// </summary>
        private void RefreshForgeDetail()
        {
            if (_selectedForgeSetting == null)
            {
                selectStateChange.SetState(0);
                return;
            }

            selectStateChange.SetState(1);

            bool unlock = _selectedForgeSetting.IsRelicUnlock();
            lockObject.SetActive(!unlock);
            _confirmForgeBtnRoot.SetActive(unlock);

            RelicItem relic = _selectedForgeSetting.GetRelicItem();
            var item = relic.ToItem();
            List<Item> materials = RelicForgeCostCalc.GetNeedItems(relic);

            if (forgeItemNameText != null)
            {
                forgeItemNameText.text = relic.LocalizeName;
                forgeItemNameText.color = relic.GetColor();
            }

            qualityText.text = $"{"QualityName".ToLocalizeStr()}: {item.quality.ToLocalizedName()}";
            RefreshAttrUI(item);

            forgeItemCell.SetItem(item, UIItemTextType.NoNum);
            forgeItemCell.btn?.onClick.RemoveAllListeners();
            if (forgeItemCell.btn != null)
            {
                forgeItemCell.btn.interactable = false;
            }

            UITool.SetCellListCount(materialSlots, materials.Count);
            ItemCell[] cells = materialSlots.GetComponentsInChildren<ItemCell>(true);
            for (int i = 0; i < materials.Count && i < cells.Length; i++)
            {
                ItemCell materialCell = cells[i];
                materialCell.SetItem(materials[i], UIItemTextType.NeedNum);
                materialCell.btn?.onClick.RemoveAllListeners();
                if (materialCell.btn != null)
                {
                    materialCell.btn.interactable = false;
                }
            }

            Inventory inventory = PlayerSaveData.LocalSavaData.inventory;
            bool isEnough = inventory.CheckEnoughItemList(materials, false);

            //state 0 = 正常可点, state 1 = 灰显(材料不足)
            //按钮本身保持可点: 材料不足时由点击后的 Toast 提示原因, 否则点了完全没反馈
            _confirmForgeBtnStateChange?.SetState(isEnough ? 0 : 1);
            confirmForgeBtn.interactable = true;
        }

        /// <summary>
        /// 刷新锻造详情的圣器属性列表，逐行显示属性名与属性值。
        /// </summary>
        private void RefreshAttrUI(Item item)
        {
            Dictionary<EAttr, float> valueDic = RelicValueCalc.GetRelicValue(item);
            List<EAttr> attrList = new List<EAttr>(valueDic.Keys);

            UITool.SetCellListCount(attrLineContainer, attrList.Count);
            for (int i = 0; i < attrList.Count; i++)
            {
                EAttr attr = attrList[i];
                // attrLine 行内文本依次为属性名与属性值
                TMP_Text[] texts = attrLineContainer.GetChild(i).GetComponentsInChildren<TMP_Text>(true);
                texts[0].text = attr.ToLocalizedName();
                texts[1].text = attr.GetEAttrNumString(valueDic[attr]);
            }
        }

        /// <summary>
        /// 点击详情界面的制作按钮，检查并消耗材料后发放圣器。
        /// </summary>
        private void OnClickConfirmForge()
        {
            TryForge(_selectedForgeSetting);
        }


        /// <summary>
        /// 检查并消耗材料，将锻造完成的圣器放入背包。
        /// </summary>
        private void TryForge(ForgeRelicSetting configRow)
        {
            if (configRow == null)
            {
                return;
            }

            Inventory inventory = PlayerSaveData.LocalSavaData.inventory;

            RelicItem relicItem = configRow.GetRelicItem();
            List<Item> materials = RelicForgeCostCalc.GetNeedItems(relicItem);
            if (!inventory.CheckEnoughItemList(materials, false))
            {
                Toast.Show(LocalizeKey.NoEnough.ToLocalizeStr(), 1);
                return;
            }

            int coinCost = 0;
            foreach (Item material in materials)
            {
                if (material != null && material.type == ItemType.Coin)
                {
                    coinCost += material.num;
                }
            }

            inventory.CheckEnoughItemList(materials, true);

            //材料里的金币是直接扣背包的, 需要手动补事件, 否则金币栏不会刷新
            if (coinCost > 0)
            {
                GameEvent.Send<int>(EGameEvent.OnCoinChange.ToInt(), -coinCost);
            }

            Item forgedItem = relicItem.ToItem();
            inventory.AddItem(forgedItem);
            PlayerSaveData.SavaData();
            //道具获得提示由 ItemPopHud 监听该事件后弹出
            GameEvent.Send<Item>(EGameEvent.OnGetItem.ToInt(), forgedItem);
            Toast.Show(LocalizeKey.ForgeSuccess.ToLocalizeStr(), 1);

            //TODO 获取UI物品特效

            RefreshRows();
            //锻造后材料数量与按钮可点状态都会变, 详情必须一起刷新
            RefreshForgeDetail();
        }


        private void OnClickBack()
        {
            UIMgr.Inst.HideView(PanelType);
        }
    }
}
