using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace XiaoCao
{
    [Serializable]
    public class Inventory
    {
        public List<Item> items = new List<Item>(); // 存储物品的列表
        public List<Item> equippedHolyRelics = new List<Item>(); // 装备中的圣器列表

        #region items

        // 添加物品到背包
        public void AddItem(ItemType itemType, string itemName, int itemCount)
        {
            Item findItem = FindItemByName(itemName);
            if (findItem != null)
            {
                findItem.num += itemCount;
                Debug.Log("Added " + itemCount + " to inventory. Count: " +findItem.num);
            }
            else
            {
                Item newItem = new Item(itemType, itemName, itemCount);
                items.Add(newItem);
                Debug.Log("Added " + newItem.ItemKey + " to inventory. Count: " + newItem.num);
            }
        }

        // 从背包中移除物品
        public void RemoveItem(string itemName)
        {
            Item itemToRemove = items.Find(item => item.ItemKey == itemName);
            if (itemToRemove != null)
            {
                items.Remove(itemToRemove);
            }
        }

        public bool CheckEnoughItem(string itemName, int amount)
        {
            Item item = FindItemByName(itemName);
            return item != null && item.num >= amount;
        }


        public bool CheckEnoughItemList(List<Item> list, bool isConsumeItem)
        {
            foreach (var item in list)
            {
                if (!CheckEnoughItem(item.ItemKey, item.num))
                {
                    return false;
                }
            }

            if (isConsumeItem)
            {
                foreach (var item in list)
                {
                    ConsumeItem(item.ItemKey, item.num);
                }
            }

            return true;
        }

        // 消耗背包中的物品
        public bool ConsumeItem(string itemName, int amount)
        {
            Item itemToConsume = FindItemByName(itemName);
            if (itemToConsume != null && itemToConsume.num >= amount)
            {
                itemToConsume.num -= amount;
                // 如果数量为零，则从背包中移除该物品
                if (itemToConsume.num == 0)
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
            return items.Find(item => item.ItemKey == itemName);
        }

        public int GetItemCount(string itemKey)
        {
            var item = FindItemByName(itemKey);
            if (item != null)
            {
                return item.num;
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
                Debug.Log("Name: " + item.ItemKey + ", Type: " + item.type + ", Count: " + item.num);
            }
        }

        #endregion

        // 装备圣器（按指定index槽位）
        public bool EquipHolyRelic(HolyRelicItem relic, int index)
        {
            var item = relic.ToItem();

            // 检查背包是否有该圣器
            var found = items.FirstOrDefault(i => i.ItemKey == item.ItemKey);
            if (found == null)
                return false;

            // 如果该槽位已有圣器，先卸载（放回背包）
            if (index >= 0 && index < equippedHolyRelics.Count)
            {
                var oldItem = equippedHolyRelics[index];
                if (oldItem != null)
                {
                    items.Add(oldItem);
                    equippedHolyRelics[index] = found;
                }
            }
            else if (index == equippedHolyRelics.Count)
            {
                // 新增一个槽位
                equippedHolyRelics.Add(found);
            }
            else
            {
                // index越界
                return false;
            }

            items.Remove(found); // 从背包移除
            return true;
        }

        // 卸载圣器
        public bool UnEquipHolyRelic(EHolyRelicType relicType)
        {
            var equipped = equippedHolyRelics.FirstOrDefault(i =>
            {
                var h = HolyRelicItem.Create(i);
                return h.relicType == relicType;
            });
            if (equipped != null)
            {
                items.Add(equipped); // 卸载后放回背包
                equippedHolyRelics.Remove(equipped);
                return true;
            }

            return false;
        }

        // 获取已装备的圣器
        public List<HolyRelicItem> GetEquippedHolyRelics()
        {
            return equippedHolyRelics.Select(i => HolyRelicItem.Create(i)).ToList();
        }
    }

    //int Level
    public enum EQuality
    {
        White = 0,
        Green = 1,
        Blue = 2,
        Purple = 3,
        Orange = 4,
        Red = 5
    }

    //圣器类型
    public enum EHolyRelicType
    {
        Might, //力量
        Resonance, //共鸣
        Primeval, // 根源
    }

    // 物品类型枚举
    public enum ItemType
    {
        None = -1, //无效
        Consumable = 0, //消耗品
        HolyRelic = 1, //装备
        Coin = 2, //钱
        Buff = 3
    }

    public struct HolyRelicItem : ISubItem
    {
        public EHolyRelicType relicType;
        public EQuality quality;
        public string modName; //圣器部件名称
        public int level; //圣器等级
        public int upgradeLevel; //强化等级


        public List<AddAttr> AddAttr { get; set; }

        public Item ToItem()
        {
            string itemId = $"{relicType}_{modName}_{upgradeLevel}";
            Item item = new Item(ItemType.Buff, itemId, level, quality);
            return item;
        }

        public static HolyRelicItem Create(Item item)
        {
            HolyRelicItem holyRelic = new HolyRelicItem();
            string[] strArray = item.typeId.Split('_');
            EHolyRelicType relicType;
            Enum.TryParse<EHolyRelicType>(strArray[0], out relicType);

            holyRelic.relicType = relicType;
            holyRelic.modName = strArray[1];
            holyRelic.quality = item.quality;
            holyRelic.level = item.num;
            holyRelic.upgradeLevel = int.Parse(strArray[2]);
            return holyRelic;
        }
    }

    public struct AddAttr
    {
        public EAttr EAttr;
        public AttributeModifier modifier;
    }


    public class HolyRelicHelper
    {
        public static EHolyRelicType GetHolyRelicType(string itemName)
        {
            if (itemName.Contains("Might"))
            {
                return EHolyRelicType.Might;
            }
            else if (itemName.Contains("Resonance"))
            {
                return EHolyRelicType.Resonance;
            }
            else if (itemName.Contains("Primeval"))
            {
                return EHolyRelicType.Primeval;
            }

            return EHolyRelicType.Might; // 默认返回
        }

        //public static string GetR
    }

    public static class HolyRelicModNames
    {
        public const string Bow = "Bow"; // 弓
        public const string HandGuard = "HandGuard"; // 护手
        public const string Knife = "Knife"; // 匕首
        public const string MagicBook = "MagicBook"; // 魔法书
        public const string Necklace = "Necklace"; // 项链
        public const string Shoese = "Shoese"; // 鞋
    }
}