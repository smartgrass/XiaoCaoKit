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

        public RelicOnBagDetail relicOnBagDetail;
        public RelicOnEquipDetail relicOnEquipDetail;

        [Header("分解相关")] 
        public GameObject breakDownUI; //分解面板
        public Button breakDownBtn; //分解入口

        public Button breakDownSureBtn; //确认分解
        public Button breakDownCancelBtn; //取消分解
        public Transform breakDownRewardRoot; //分解产出预览的格子容器

        [Header("模式互斥显示")]
        public GameObject leftPanel; //正常模式的左侧圣器装备区
        public GameObject rightBtnGroup; //正常模式的右侧按钮组


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
            breakDownSureBtn.onClick.AddListener(OnBreakDownSureClick);
            breakDownCancelBtn.onClick.AddListener(OnBreakDownCancelClick);
        }

        public override void OnEnable()
        {
            //每次打开面板都回到普通模式,并清空上一次遗留的分解勾选
            _breakDownItems.Clear();
            SetBreakDownMode(false);
            relicOnBagDetail.gameObject.SetActive(false);
            relicOnEquipDetail.gameObject.SetActive(false);

            UpdateUI();
        }

        private void OnDestroy()
        {
            quickEquipBtn.onClick.RemoveListener(OnQuickEquipClick);
            quickUnEquipBtn.onClick.RemoveListener(OnQuickUnEquipClick);
            breakDownBtn.onClick.RemoveListener(OnBreakDownClick);
            breakDownSureBtn.onClick.RemoveListener(OnBreakDownSureClick);
            breakDownCancelBtn.onClick.RemoveListener(OnBreakDownCancelClick);
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
            //先切换面板显隐再刷新内容,避免退出分解时左侧面板还处于隐藏状态导致刷新被跳过
            SetBreakDownMode(_isBreakDownMode);
            ShowEquippedRelics();
            ShowBagRelics();
            ShowPlayerAttr();
            RefreshBreakDownReward();
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
                BindEquipCell(cell);
            }

            for (int i = showCount; i < cells.Length; i++)
            {
                ShowEquipCell(cells[i], null);
                BindEquipCell(cells[i]);
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

            SetCellSelected(cell, _breakDownItems.Contains(item));
        }
        
        private void BindEquipCell(EquipRelicCell cell)
        {
            if (cell?.itemCell?.btn == null)
            {
                return;
            }

            cell.itemCell.btn.onClick.RemoveAllListeners();
            cell.itemCell.btn.onClick.AddListener(() =>
                {
                    relicOnEquipDetail.Show(cell.itemCell.Item);
                    relicOnEquipDetail.SetDataChangeAction(OnEquipDataChange);
                }
            );
        }

        void OnEquipDataChange(Item item)
        {
            UpdateUI();
        }

        /// <summary>
        /// 显示 Hp、Atk、Def 三项角色属性。
        /// </summary>
        private void ShowPlayerAttr()
        {
            TMP_Text[] texts = attrTextContainer.GetComponentsInChildren<TMP_Text>(false);
            if (texts.Length == 0)
            {
                return;
            }

            RoleAttr attr = PlayerSaveData.GetInitPlayerAttr(false);
            string[] attrTexts =
            {
                $"{("Hp".ToLocalizeStr())}:{Mathf.RoundToInt(attr.hp)}/{Mathf.RoundToInt(attr.MaxHp)}",
                $"{("Atk".ToLocalizeStr())}:{attr.Atk}",
                $"{("Def".ToLocalizeStr())}:{attr.Def}"
            };

            int showCount = Mathf.Min(texts.Length, attrTexts.Length);
            for (int i = 0; i < showCount; i++)
            {
                texts[i].text = attrTexts[i];
            }
        }

        /// <summary>
        /// 一键卸下所有已装备圣器并刷新属性缓存。
        /// </summary>
        private void OnQuickUnEquipClick()
        {
            if (PlayerSaveData == null)
            {
                return;
            }

            EnsureInventoryData();
            List<Item> equippedItems = PlayerSaveData.inventory.equippedHolyRelics;
            for (int i = equippedItems.Count - 1; i >= 0; i--)
            {
                Item item = equippedItems[i];
                if (item != null)
                {
                    PlayerSaveData.inventory.items.Add(item);
                }
            }

            equippedItems.Clear();
            SaveRelicData();
            UpdateUI();
        }

        /// <summary>
        /// 点击一键装备，为每种圣器在背包与已装备之间选出综合等级最高的一个。
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

                //只有背包里的最优圣器优于当前已装备的圣器时才替换
                if (bestItem == null || !IsBetterRelic(bestItem, GetEquippedRelic(relicType)))
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
            cell.qualityBg.enabled = true;
            cell.btn.interactable = true;

            //分解模式下的勾选状态跟随选中集合,避免格子复用时残留打勾
            cell.SetMask("hasGet", _breakDownItems.Contains(item));
        }

        /// <summary>
        /// 清理空背包格子，避免复用格子时残留圣器显示。
        /// </summary>
        private void ClearBagRelicCell(ItemCell cell)
        {
            if (cell == null)
            {
                return;
            }

            cell.clickMode = EItemClickMode.None;
            cell.clickAction = null;
            cell.SetMask("hasGet", false);
            cell.icon.sprite = null;
            cell.icon.enabled = false;
            cell.qualityBg.enabled = false;
            cell.text.text = string.Empty;
            cell.levelText.gameObject.SetActive(false);
            cell.btn.interactable = false;
        }
        
        private void OnBagRelicCellClick(ItemCell cell)
        {
            if (_isBreakDownMode)
            {
                //分解模式下点击切换选中状态,并同步打勾与产出预览
                if (_breakDownItems.Remove(cell.Item))
                {
                    cell.SetMask("hasGet", false);
                }
                else
                {
                    _breakDownItems.Add(cell.Item);
                    cell.SetMask("hasGet", true);
                }

                RefreshBreakDownReward();
                return;
            }

            relicOnBagDetail.Show(cell.Item);
            relicOnBagDetail.SetEquipAction(EquipRelicCallBlack);
        }

        private void EquipRelicCallBlack(Item item)
        {
            if (TryGetRelicType(item, out ERelicType equippedType))
            {
                EquipRelic(item, equippedType);
                SaveRelicData();
                UpdateUI();
            }
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
        /// 完成当前选中的圣器分解：消耗选中的圣器，并按 ForgeCostCalc 的规则发放分解产物。
        /// </summary>
        private void OnBreakDownSureClick()
        {
            if (PlayerSaveData == null)
            {
                return;
            }

            EnsureInventoryData();
            Inventory inventory = PlayerSaveData.inventory;

            //先复制一份选中项,避免遍历过程中修改集合
            List<Item> breakDownList = _breakDownItems.Where(item => item != null).ToList();
            if (breakDownList.Count == 0)
            {
                return;
            }

            int breakDownCount = 0;
            foreach (Item item in breakDownList)
            {
                if (!inventory.items.Remove(item) && !inventory.equippedHolyRelics.Remove(item))
                {
                    //既不在背包也不在装备位,说明数据已经变化,跳过
                    continue;
                }

                breakDownCount++;
                foreach (Item reward in RelicForgeCostCalc.GetBreakDownItems(item.ToRelicItem()))
                {
                    inventory.AddItem(reward);
                }
            }

            if (breakDownCount == 0)
            {
                return;
            }

            Debug.Log($"圣器分解完成,共分解 {breakDownCount} 件");

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
        /// 按当前选中的圣器刷新分解产出预览，相同材料会合并数量。
        /// </summary>
        private void RefreshBreakDownReward()
        {
            Dictionary<string, Item> rewardDic = new Dictionary<string, Item>();
            foreach (Item item in _breakDownItems)
            {
                foreach (Item reward in RelicForgeCostCalc.GetBreakDownItems(item.ToRelicItem()))
                {
                    if (rewardDic.TryGetValue(reward.ItemKey, out Item existReward))
                    {
                        existReward.num += reward.num;
                    }
                    else
                    {
                        rewardDic.Add(reward.ItemKey, reward);
                    }
                }
            }

            //按类型和品级排序,避免HashSet遍历顺序不同导致预览每次跳位
            List<Item> rewards = rewardDic.Values
                .OrderBy(reward => reward.type)
                .ThenByDescending(reward => reward.quality)
                .ToList();

            UITool.SetCellListCount(breakDownRewardRoot, rewards.Count);

            ItemCell[] cells = breakDownRewardRoot.GetComponentsInChildren<ItemCell>(true);
            for (int i = 0; i < rewards.Count && i < cells.Length; i++)
            {
                SetRewardCell(cells[i], rewards[i]);
            }
        }

        /// <summary>
        /// 写入单个分解产出格子的数量与品质底色。
        /// </summary>
        private static void SetRewardCell(ItemCell cell, Item item)
        {
            cell.SetItem(item);
            cell.qualityBg.enabled = true;
        }

        /// <summary>
        /// 切换分解模式：分解面板与正常模式的装备区/按钮组互斥显示。
        /// </summary>
        private void SetBreakDownMode(bool isBreakDownMode)
        {
            _isBreakDownMode = isBreakDownMode;
            breakDownUI.SetActive(_isBreakDownMode);
            leftPanel.SetActive(!_isBreakDownMode);
            rightBtnGroup.SetActive(!_isBreakDownMode);
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
        /// 判断候选圣器是否严格优于当前圣器，比较规则与背包排序一致：先比综合等级，再比品质。
        /// </summary>
        private static bool IsBetterRelic(Item candidate, Item current)
        {
            if (candidate == null)
            {
                return false;
            }

            if (current == null)
            {
                return true;
            }

            int candidateScore = GetRelicScore(candidate);
            int currentScore = GetRelicScore(current);
            if (candidateScore != currentScore)
            {
                return candidateScore > currentScore;
            }

            return (int)candidate.quality > (int)current.quality;
        }

        /// <summary>
        /// 同步装备数据并写入本地存档。
        /// </summary>
        private void SaveRelicData()
        {
            PlayerSaveData.ReLoadPlayerAttr();
            PlayerSaveData.SavaData();
        }
    }
}
