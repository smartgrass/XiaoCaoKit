using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public class InventoryUI : MonoBehaviour
    {
        public Inventory localInventory => PlayerSaveData.LocalSavaData.inventory;
        public GameObject slotPrefab; // 格子预制体
        public Transform slotsParent; // 格子的父对象
        private List<InventorySlotUI> slots = new List<InventorySlotUI>();

        void Start()
        {
            // 在开始时动态生成格子
            InitializeSlots();
        }

        // 动态生成格子
        private void InitializeSlots()
        {
            for (int i = 0; i < localInventory.items.Count; i++)
            {
                GameObject slotObject = Instantiate(slotPrefab, slotsParent);
                InventorySlotUI slot = slotObject.GetComponent<InventorySlotUI>();
                slots.Add(slot);
            }
        }

        // 更新所有格子的显示
        public void UpdateUI()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (i < localInventory.items.Count)
                {
                    slots[i].UpdateSlot(localInventory.items[i]);
                }
                else
                {
                    // 如果格子数量超过物品数量，隐藏多余的格子
                    slots[i].UpdateSlot(null);
                }
            }
        }
    }


}
