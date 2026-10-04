using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 已装备的圣器详情界面，负责窗口切换、卸载和升级业务。
    /// </summary>
    public class RelicOnEquipDetail : MonoBehaviour
    {
        public RelicOnEquipDetailShowUI onEquipDetailShowUI;
        public RelicOnEquipDetailUpgradeUI onEquipDetailUpgradeUI;

        /// <summary>
        /// 当前正在显示的物品。
        /// </summary>
        public Item CurrentItem { get; private set; }

        private Action<Item> _dataChangeAction;
        
        private void Awake()
        {
            onEquipDetailShowUI?.SetActions(OnUnEquip, ShowUpgradeWindow, Hide);
            onEquipDetailUpgradeUI?.SetActions(OnConfirmUpgrade, ShowDetailWindow, Hide);
        }

        /// <summary>
        /// 显示指定物品的详情。
        /// </summary>
        /// <param name="item">需要显示的圣器。</param>
        public void Show(Item item)
        {
            gameObject.SetActive(true);
            SetItem(item);
            ShowDetailWindow();
        }

        /// <summary>
        /// 设置卸载完成后的外部回调。
        /// </summary>
        public void SetDataChangeAction(Action<Item> action)
        {
            _dataChangeAction = action;
        }
        

        /// <summary>
        /// 刷新当前圣器数据。
        /// </summary>
        public void SetItem(Item item)
        {
            CurrentItem = item;
            if (item == null)
            {
                Clear();
                return;
            }

            onEquipDetailShowUI?.Refresh(item);
            onEquipDetailUpgradeUI?.Refresh(item);
        }

        /// <summary>
        /// 隐藏详情界面并清理当前数据。
        /// </summary>
        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 响应普通窗口的卸载按钮。
        /// </summary>
        private void OnUnEquip()
        {
            if (CurrentItem == null)
            {
                return;
            }
            
            DefaultUnEquip(CurrentItem);
            
            if (_dataChangeAction != null)
            {
                _dataChangeAction.Invoke(CurrentItem);
            }
            
            Hide();
        }

        /// <summary>
        /// 显示升级窗口。
        /// </summary>
        private void ShowUpgradeWindow()
        {
            if (CurrentItem == null)
            {
                return;
            }

            onEquipDetailShowUI?.Hide();
            onEquipDetailUpgradeUI?.Show(CurrentItem);
        }

        /// <summary>
        /// 返回普通详情窗口。
        /// </summary>
        private void ShowDetailWindow()
        {
            onEquipDetailShowUI?.Show();
            onEquipDetailUpgradeUI?.Hide();
        }

        /// <summary>
        /// 响应升级窗口的确认按钮并提升圣器等级。
        /// </summary>
        private void OnConfirmUpgrade()
        {
            if (!TryUpgradeCurrentItem())
            {
                return;
            }

            SaveRelicData();
            DefaultUnEquip(CurrentItem);
            SetItem(CurrentItem);
            onEquipDetailUpgradeUI?.Show(CurrentItem);
        }

        /// <summary>
        /// 将当前圣器提升一级。
        /// </summary>
        private bool TryUpgradeCurrentItem()
        {
            if (CurrentItem == null || CurrentItem.type != ItemType.HolyRelic)
            {
                return false;
            }

            RelicItem relicItem = CurrentItem.ToRelicItem();
            RelicDetailHelper.SetRelicLevel(CurrentItem, relicItem.level + 1);
            return true;
        }

        /// <summary>
        /// 未接入外部回调时，将当前圣器从装备列表移回背包。
        /// </summary>
        private static void DefaultUnEquip(Item item)
        {
            PlayerSaveData playerSaveData = GameAllData.playerSaveData;
            if (playerSaveData?.inventory == null || item == null)
            {
                return;
            }

            playerSaveData.inventory.items ??= new List<Item>();
            playerSaveData.inventory.equippedHolyRelics ??= new List<Item>();
            if (!playerSaveData.inventory.equippedHolyRelics.Remove(item))
            {
                return;
            }

            playerSaveData.inventory.items.Add(item);
            SaveRelicData();
        }

        /// <summary>
        /// 刷新圣器属性缓存并写入本地存档。
        /// </summary>
        private static void SaveRelicData()
        {
            PlayerSaveData playerSaveData = GameAllData.playerSaveData;
            if (playerSaveData == null)
            {
                return;
            }

            playerSaveData.ReLoadPlayerAttr();
            playerSaveData.SavaThisData();
        }

        /// <summary>
        /// 清理当前圣器和外部回调。
        /// </summary>
        private void Clear()
        {
            CurrentItem = null;
            onEquipDetailShowUI?.Hide();
            onEquipDetailUpgradeUI?.Hide();
        }
    }
}
