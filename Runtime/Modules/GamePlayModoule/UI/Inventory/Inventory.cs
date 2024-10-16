using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;

namespace XiaoCao
{
    [Serializable]
    public class Inventory
    {
        public List<Item> items = new List<Item>(); // 存储物品的列表

        // 添加物品到背包
        public void AddItem(ItemType itemType, string itemName, int itemCount)
        {
            Item newItem = new Item(itemType, itemName, itemCount);
            items.Add(newItem);
            Debug.Log("Added " + newItem.Key + " to inventory. Count: " + newItem.count);
        }

        // 从背包中移除物品
        public void RemoveItem(string itemName)
        {
            Item itemToRemove = items.Find(item => item.Key == itemName);
            if (itemToRemove != null)
            {
                items.Remove(itemToRemove);
            }
        }


        public bool CheckEnoughItem(string itemName, int amount)
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
            return items.Find(item => item.Key == itemName);
        }

        public int GetItemCount(string itemKey)
        {
            var item = FindItemByName(itemKey);
            if (item != null)
            {
                return item.count;
            }
            else
            {
                return 0;
            }
        }

        // 显示背包中的物品
        public void DisplayInventory()
        {
            Debug.Log("Inventory Contents:");
            foreach (Item item in items)
            {
                Debug.Log("Name: " + item.Key + ", Type: " + item.type + ", Count: " + item.count);
            }
        }
    }

    public enum EQuality
    {
        Null = -1,
        White = 0,
        Green = 1,
        Blue = 2,
        Purple = 3,
    }

    public enum EEquipmentSub
    {
        Weapon = 0,
        Clothes = 1,

    }

    // 物品类，包含类型、数量和名字属性
    [System.Serializable]
    public class Item
    {
        public ItemType type;
        public string id;
        public int count;
        public EQuality Quality { get; set; }

        public string Key
        {
            get
            {
                if (type == ItemType.Coin)
                {
                    return type.ToString();
                }
                return $"{id}_{Quality}";
            }
        }

        public Item(ItemType itemType, string itemId, int itemCount)
        {
            id = itemId;
            type = itemType;
            count = itemCount;

            if (itemType != ItemType.Coin && itemId.Contains("_"))
            {
               int qNum =  GetQualityNum(itemId);
                Quality = (EQuality)qNum;
            }
            else
            {
                Quality = EQuality.White;
            }

        }


        int GetQualityNum(string input)
        {
            const string pattern = @"_(\d+)$";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(input);
            if (match.Success)
            {
                string numberStr = match.Groups[1].Value;
                int number = 0;
                int.TryParse(numberStr, out number);
                return number;
            }
            return 0;
        }

    }

    // 物品类型枚举
    public enum ItemType
    {
        Consumable = 0, //消耗品
        Equipment = 1, //装备
        Coin = 2, //钱
    }

    public enum EquipType
    {
        Weapon, //武器
        Armor //道具
    }
}