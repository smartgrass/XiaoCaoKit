using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    [Serializable]
    public class Inventory
    {
        public List<Item> items = new List<Item>(); // 存储物品的列表

        // 添加物品到背包
        public void AddItem(ItemType itemType, string itemName,  int itemCount)
        {
            Item newItem = new Item(itemType, itemName,  itemCount);
            items.Add(newItem);
            Debug.Log("Added " + newItem.name + " to inventory. Count: " + newItem.count);
        }

        // 从背包中移除物品
        public void RemoveItem(string itemName)
        {
            Item itemToRemove = items.Find(item => item.name == itemName);
            if (itemToRemove != null)
            {
                items.Remove(itemToRemove);
            }
        }


        public bool CheckItemAvailability(string itemName, int amount)
        {
            Item item = FindItemByName(itemName);
            return item != null && item.count >= amount;
        }

        // 消耗背包中的物品
        public bool ConsumeItem(string itemName, int amount)
        {
            Item itemToConsume = FindItemByName(itemName);
            if (itemToConsume != null && itemToConsume.count >= amount)
            {
                itemToConsume.count -= amount;
                // 如果数量为零，则从背包中移除该物品
                if (itemToConsume.count == 0)
                {
                    RemoveItem(itemName);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private Item FindItemByName(string itemName)
        {
            return items.Find(item => item.name == itemName);
        }


        // 显示背包中的物品
        public void DisplayInventory()
        {
            Debug.Log("Inventory Contents:");
            foreach (Item item in items)
            {
                Debug.Log("Name: " + item.name + ", Type: " + item.type + ", Count: " + item.count);
            }
        }
    }

    // 物品类，包含类型、数量和名字属性
    [System.Serializable]
    public class Item
    {
        public ItemType type;
        public string name;
        public int count;

        public Item( ItemType itemType, string itemName, int itemCount)
        {
            name = itemName;
            type = itemType;
            count = itemCount;
        }
    }

    // 物品类型枚举
    public enum ItemType
    {
        Consumable = 0, //消耗品
        Equipment = 1, //装备
    }

    public enum EquipType
    {
        Weapon, //武器
        Armor //道具
    }
}
