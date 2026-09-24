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
    /// 圣器装备界面，负责装备槽、角色属性和圣器操作。
    /// </summary>
    public class RelicView : EnableShowUI
    {
        public Transform relicCellRoot;
        public Transform bagRelicCellRoot;
        public Transform attrTextContainer;

        public Button quickEquipBtn;
        public Button quickUnEquipBtn;
        public Button breakDownBtn;

        public Button breakDownSureBtn;
        public Button breakDownCancelBtn;

        public int baseCount = 24;

        //ui显示排序
        public static ERelicType[] TypeList =
        {
            ERelicType.Helmet, ERelicType.Armor,
            ERelicType.Shoe, ERelicType.Weapon, ERelicType.Jewelry
        };

        private readonly HashSet<Item> _breakDownItems = new HashSet<Item>();
        private readonly List<ItemCell> _bagRelicCells = new List<ItemCell>();
        private List<Item> _sortedBagRelics = new List<Item>();
        private bool _isBreakDownMode;

        private PlayerSaveData PlayerSaveData => GameAllData.playerSaveData;

        private void Awake()
        {
            quickEquipBtn.onClick.AddListener(OnQuickEquipClick);
            quickUnEquipBtn.onClick.AddListener(OnQuickUnEquipClick);
            breakDownBtn.onClick.AddListener(OnBreakDownClick);
            breakDownSureBtn?.onClick.AddListener(OnBreakDownSureClick);
            breakDownCancelBtn?.onClick.AddListener(OnBreakDownCancelClick);
        }

        public override void OnEnable()
        {
            _breakDownItems.Clear();
            SetBreakDownMode(false);
            UpdateUI();
        }

        private void OnDestroy()
        {
            quickEquipBtn?.onClick.RemoveListener(OnQuickEquipClick);
            breakDownBtn?.onClick.RemoveListener(OnBreakDownClick);
            breakDownSureBtn?.onClick.RemoveListener(OnBreakDownSureClick);
            breakDownCancelBtn?.onClick.RemoveListener(OnBreakDownCancelClick);
        }

        /// <summary>
        /// 刷新装备槽、角色属性以及当前分解模式的显示状态。
        /// </summary>
        public override void UpdateUI()
        {
            if (PlayerSaveData == null)
            {
                return;
            }

            EnsureInventoryData();
            _sortedBagRelics = GetSortedBagRelics();
            ShowEquippedRelics();
            ShowBagRelics();
            ShowPlayerAttr();
            SetBreakDownMode(_isBreakDownMode);
        }

        /// <summary>
        /// 确保背包和旧版存档字段可用，并兼容旧的装备圣器数据。
        /// </summary>
        private void EnsureInventoryData()
        {
            PlayerSaveData.inventory ??= new Inventory();
            PlayerSaveData.inventory.items ??= new List<Item>();
            PlayerSaveData.inventory.equippedHolyRelics ??= new List<Item>();
        }

        /// <summary>
        /// 按 TypeList 的顺序显示五个装备槽。
        /// </summary>
        private void ShowEquippedRelics()
        {
            if (relicCellRoot == null)
            {
                return;
            }

            EquipRelicCell[] cells = relicCellRoot.GetComponentsInChildren<EquipRelicCell>(true)
                .OrderBy(cell => cell.transform.GetSiblingIndex())
                .ToArray();

            int showCount = Mathf.Min(TypeList.Length, cells.Length);
            for (int i = 0; i < showCount; i++)
            {
                EquipRelicCell cell = cells[i];
                cell.UIIndex = i;

                Item item = GetEquippedRelic(TypeList[i]);
                ShowEquipCell(cell, item);
                BindEquipCell(cell, item);
            }

            for (int i = showCount; i < cells.Length; i++)
            {
                ShowEquipCell(cells[i], null);
                BindEquipCell(cells[i], null);
            }
        }

        /// <summary>
        /// 写入单个装备槽的物品、图标和空槽状态。
        /// </summary>
        private void ShowEquipCell(EquipRelicCell cell, Item item)
        {
            if (cell == null)
            {
                return;
            }

            bool hasItem = item != null && item.type == ItemType.HolyRelic;
            if (cell.emptyShow != null)
            {
                cell.emptyShow.SetActive(!hasItem);
            }

            if (cell.itemCell == null)
            {
                return;
            }

            cell.itemCell.gameObject.SetActive(hasItem);
            if (!hasItem)
            {
                SetCellSelected(cell, false);
                return;
            }

            cell.itemCell.SetItem(item, UIItemTextType.NoNum);
            if (cell.itemCell.icon != null)
            {
                cell.itemCell.icon.sprite = item.GetItemSprite();
                cell.itemCell.icon.enabled = cell.itemCell.icon.sprite != null;
            }

            SetCellSelected(cell, _breakDownItems.Contains(item));
        }

        /// <summary>
        /// 绑定装备槽在分解模式下的点击事件。
        /// </summary>
        private void BindEquipCell(EquipRelicCell cell, Item item)
        {
            if (cell?.itemCell?.btn == null)
            {
                return;
            }

            cell.itemCell.btn.onClick.RemoveAllListeners();
            cell.itemCell.btn.interactable = item != null && _isBreakDownMode;
            if (item == null)
            {
                return;
            }

            Item selectedItem = item;
            cell.itemCell.btn.onClick.AddListener(() => OnEquipCellClick(selectedItem, cell));
        }

        /// <summary>
        /// 显示 Hp、Atk、Def 三项角色属性。
        /// </summary>
        private void ShowPlayerAttr()
        {
            if (attrTextContainer == null)
            {
                return;
            }

            TMP_Text[] texts = attrTextContainer.GetComponentsInChildren<TMP_Text>(false);
            if (texts.Length == 0)
            {
                return;
            }

            PlayerAttr attr = PlayerSaveData.GetInitPlayerAttr(false);
            string[] attrTexts =
            {
                $"{("Hp".ToLocalizeStr())}:{attr.hp:0.##}/{attr.MaxHp:0.##}",
                $"{("Atk".ToLocalizeStr())}:{attr.Atk}",
                $"{("Def".ToLocalizeStr())}:{attr.Def}"
            };

            int showCount = Mathf.Min(texts.Length, attrTexts.Length);
            for (int i = 0; i < showCount; i++)
            {
                texts[i].text = attrTexts[i];
            }
        }

        //一键卸载
        private void OnQuickUnEquipClick()
        {
        }

        /// <summary>
        /// 点击一键装备，为每种圣器选择综合等级最高的背包物品。
        /// </summary>
        private void OnQuickEquipClick()
        {
            if (PlayerSaveData == null)
            {
                return;
            }

            EnsureInventoryData();
            _sortedBagRelics = GetSortedBagRelics();
            for (int i = 0; i < TypeList.Length; i++)
            {
                ERelicType relicType = TypeList[i];
                Item bestItem = _sortedBagRelics
                    .FirstOrDefault(item =>
                        TryGetRelicType(item, out ERelicType itemType) && itemType == relicType);

                if (bestItem == null)
                {
                    continue;
                }

                EquipRelic(bestItem, relicType);
            }

            SaveRelicData();
            UpdateUI();
        }

        /// <summary>
        /// 获取背包内的全部圣器，并按综合等级从高到低排序。
        /// </summary>
        private List<Item> GetSortedBagRelics()
        {
            HashSet<string> equippedRelicKeys = new HashSet<string>(
                PlayerSaveData.inventory.equippedHolyRelics
                    .Where(item => item != null)
                    .Select(item => item.ItemKey));

            return PlayerSaveData.inventory.items
                .Where(item => TryGetRelicType(item, out _) &&
                               !equippedRelicKeys.Contains(item.ItemKey))
                .OrderByDescending(GetRelicScore)
                .ThenByDescending(item => (int)item.quality)
                .ToList();
        }

        /// <summary>
        /// 显示背包中尚未装备的圣器列表。
        /// </summary>
        private void ShowBagRelics()
        {
            if (bagRelicCellRoot == null)
            {
                return;
            }

            int hasCount = _sortedBagRelics.Count;
            int showCount = Math.Max(baseCount, hasCount);

            _bagRelicCells.Clear();
            UITool.SetCellListCount(bagRelicCellRoot, showCount);
            _bagRelicCells.AddRange(bagRelicCellRoot.GetComponentsInChildren<ItemCell>(false));

            if (_bagRelicCells.Count < showCount)
            {
                Debug.LogError($"圣器背包容器配置错误，ItemCell数量不足：{_bagRelicCells.Count}/{showCount}");
                return;
            }

            for (int i = 0; i < showCount; i++)
            {
                ItemCell cell = _bagRelicCells[i];
                if (i < hasCount)
                {
                    SetBagRelicCell(cell, _sortedBagRelics[i]);
                }
                else
                {
                    ClearBagRelicCell(cell);
                }

                cell.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 写入背包圣器格子的物品数据、图标和点击行为。
        /// </summary>
        private void SetBagRelicCell(ItemCell cell, Item item)
        {
            if (cell == null)
            {
                return;
            }

            cell.SetItem(item, UIItemTextType.NoNum);
            cell.clickMode = EItemClickMode.Custom;
            cell.clickAction = OnBagRelicCellClick;

            if (cell.icon != null)
            {
                cell.icon.sprite = item.GetItemSprite();
                cell.icon.enabled = cell.icon.sprite != null;
            }

            if (cell.qualityBg != null)
            {
                cell.qualityBg.enabled = true;
            }

            if (cell.btn != null)
            {
                cell.btn.interactable = !_isBreakDownMode;
            }
        }

        /// <summary>
        /// 清理空背包格子，避免复用格子时残留圣器显示。
        /// </summary>
        private static void ClearBagRelicCell(ItemCell cell)
        {
            if (cell == null)
            {
                return;
            }

            cell.clickMode = EItemClickMode.None;
            cell.clickAction = null;

            if (cell.icon != null)
            {
                cell.icon.sprite = null;
                cell.icon.enabled = false;
            }

            if (cell.qualityBg != null)
            {
                cell.qualityBg.enabled = false;
            }

            if (cell.text != null)
            {
                cell.text.text = string.Empty;
            }

            if (cell.btn != null)
            {
                cell.btn.interactable = false;
            }
        }

        /// <summary>
        /// 点击背包圣器时装备到对应槽位。
        /// </summary>
        private void OnBagRelicCellClick(ItemCell cell)
        {
            if (_isBreakDownMode || cell == null)
            {
                return;
            }

            Item item = cell.Item;
            if (!TryGetRelicType(item, out ERelicType relicType))
            {
                return;
            }

            EquipRelic(item, relicType);
            SaveRelicData();
            UpdateUI();
        }

        /// <summary>
        /// 将指定圣器替换到对应类型的装备槽，并把旧装备放回背包。
        /// </summary>
        private void EquipRelic(Item item, ERelicType relicType)
        {
            List<Item> equippedItems = PlayerSaveData.inventory.equippedHolyRelics;
            for (int i = equippedItems.Count - 1; i >= 0; i--)
            {
                Item equippedItem = equippedItems[i];
                if (!TryGetRelicType(equippedItem, out ERelicType equippedType) ||
                    equippedType != relicType)
                {
                    continue;
                }

                if (equippedItem != item)
                {
                    PlayerSaveData.inventory.items.Add(equippedItem);
                }

                equippedItems.RemoveAt(i);
            }

            PlayerSaveData.inventory.items.Remove(item);
            int insertIndex = GetEquippedInsertIndex(relicType, equippedItems);
            equippedItems.Insert(insertIndex, item);
        }

        /// <summary>
        /// 按 TypeList 计算装备圣器在列表中的插入位置。
        /// </summary>
        private int GetEquippedInsertIndex(ERelicType relicType, List<Item> equippedItems)
        {
            int targetOrder = Array.IndexOf(TypeList, relicType);
            for (int i = 0; i < equippedItems.Count; i++)
            {
                if (!TryGetRelicType(equippedItems[i], out ERelicType currentType))
                {
                    continue;
                }

                if (Array.IndexOf(TypeList, currentType) > targetOrder)
                {
                    return i;
                }
            }

            return equippedItems.Count;
        }

        /// <summary>
        /// 进入圣器分解模式。
        /// </summary>
        private void OnBreakDownClick()
        {
            _breakDownItems.Clear();
            SetBreakDownMode(true);
            UpdateUI();
        }

        /// <summary>
        /// 完成当前选中的圣器分解。分解产物暂不发放。
        /// </summary>
        private void OnBreakDownSureClick()
        {
            if (PlayerSaveData == null)
            {
                return;
            }

            EnsureInventoryData();
            for (int i = PlayerSaveData.inventory.equippedHolyRelics.Count - 1; i >= 0; i--)
            {
                Item item = PlayerSaveData.inventory.equippedHolyRelics[i];
                if (_breakDownItems.Contains(item))
                {
                    PlayerSaveData.inventory.equippedHolyRelics.RemoveAt(i);
                }
            }

            _breakDownItems.Clear();
            SetBreakDownMode(false);
            SaveRelicData();
            UpdateUI();
        }

        /// <summary>
        /// 取消当前圣器分解选择。
        /// </summary>
        private void OnBreakDownCancelClick()
        {
            _breakDownItems.Clear();
            SetBreakDownMode(false);
            UpdateUI();
        }

        /// <summary>
        /// 切换分解模式下的按钮和装备槽交互状态。
        /// </summary>
        private void SetBreakDownMode(bool isBreakDownMode)
        {
            _isBreakDownMode = isBreakDownMode;
            quickEquipBtn?.gameObject.SetActive(!isBreakDownMode);
            breakDownBtn?.gameObject.SetActive(!isBreakDownMode);
            breakDownSureBtn?.gameObject.SetActive(isBreakDownMode);
            breakDownCancelBtn?.gameObject.SetActive(isBreakDownMode);
        }

        /// <summary>
        /// 选择或取消选择一个待分解圣器。
        /// </summary>
        private void OnEquipCellClick(Item item, EquipRelicCell cell)
        {
            if (!_isBreakDownMode || item == null)
            {
                return;
            }

            if (!_breakDownItems.Add(item))
            {
                _breakDownItems.Remove(item);
            }

            SetCellSelected(cell, _breakDownItems.Contains(item));
        }

        /// <summary>
        /// 使用装备槽预制体中的边框显示选中状态。
        /// </summary>
        private void SetCellSelected(EquipRelicCell cell, bool isSelected)
        {
            Transform outline = cell?.transform.Find("outLine");
            if (outline != null)
            {
                outline.gameObject.SetActive(isSelected);
            }
        }

        /// <summary>
        /// 获取指定类型的当前装备圣器。
        /// </summary>
        private Item GetEquippedRelic(ERelicType relicType)
        {
            return PlayerSaveData.inventory.equippedHolyRelics.FirstOrDefault(item =>
                TryGetRelicType(item, out ERelicType itemType) && itemType == relicType);
        }

        /// <summary>
        /// 判断物品是否为圣器并解析其类型。
        /// </summary>
        private static bool TryGetRelicType(Item item, out ERelicType relicType)
        {
            relicType = default;
            if (item == null || item.type != ItemType.HolyRelic || string.IsNullOrEmpty(item.typeId))
            {
                return false;
            }

            string typeName = item.typeId.Split('_')[0].Replace("Relic/", string.Empty);
            return Enum.TryParse(typeName, out relicType);
        }

        /// <summary>
        /// 计算圣器综合等级：圣器等级加品质乘十。
        /// </summary>
        private static int GetRelicScore(Item item)
        {
            if (item == null)
            {
                return int.MinValue;
            }

            return item.num + (int)item.quality * 10;
        }

        /// <summary>
        /// 同步装备数据并写入本地存档。
        /// </summary>
        private void SaveRelicData()
        {
            PlayerSaveData.SavaData();
        }
    }
}