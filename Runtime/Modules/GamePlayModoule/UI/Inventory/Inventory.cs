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

        /// <summary>
        /// 将完整物品数据加入背包，并按物品键合并相同物品。
        /// </summary>
        public void AddItem(Item item)
        {
            if (item == null || item.num <= 0)
            {
                return;
            }

            Item findItem = items.Find(inventoryItem =>
                inventoryItem != null && inventoryItem.ItemKey == item.ItemKey);
            if (findItem != null)
            {
                findItem.num += item.num;
                return;
            }

            items.Add(new Item(item.type, item.typeId, item.num, item.quality));
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
        public bool EquipHolyRelic(RelicItem relic, int index)
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
        public bool UnEquipHolyRelic(ERelicType relicType)
        {
            var equipped = equippedHolyRelics.FirstOrDefault(i =>
            {
                var h = RelicItem.Create(i);
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
        public List<RelicItem> GetEquippedHolyRelics()
        {
            return equippedHolyRelics.Select(i => RelicItem.Create(i)).ToList();
        }
    }

    //int Level
    public enum EQuality
    {
        White = 0, //普通
        Green = 1, //精良
        Blue = 2, //稀有
        Purple = 3, //史诗
        Orange = 4, //传说
        Red = 5 //神话
    }

    //圣器类型
    public enum ERelicType
    {
        Weapon, //武器
        Helmet, //头盔
        Armor, //盔甲
        Shoe, //靴子
        Jewelry, //饰品
    }

    // 物品类型枚举
    public enum ItemType
    {
        None = -1, //无效
        Consumable = 0, //消耗品
        HolyRelic = 1, //圣器
        Coin = 2, //钱
        Buff = 3, //纹章
        ExtraItem = 4 , //关卡内可以使用的道具
    }

    public struct RelicItem : ISubItem
    {
        public ERelicType relicType;
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

        public static RelicItem Create(Item item)
        {
            RelicItem relic = new RelicItem();
            string[] strArray = item.typeId.Split('_');
            ERelicType relicType;
            Enum.TryParse<ERelicType>(strArray[0], out relicType);

            relic.relicType = relicType;
            relic.modName = strArray[1];
            relic.quality = item.quality;
            relic.level = item.num;
            relic.upgradeLevel = int.Parse(strArray[2]);
            return relic;
        }
    }

    public struct AddAttr
    {
        public EAttr EAttr;
        public AttributeModifier modifier;
    }


    public class HolyRelicHelper
    {
        public static ERelicType GetHolyRelicType(string itemName)
        {
            if (itemName.Contains("Relic/Jewelry"))
            {
                return ERelicType.Jewelry;
            }
            else if (itemName.Contains("Relic/Shoe"))
            {
                return ERelicType.Shoe;
            }
            else if (itemName.Contains("Relic/Armor"))
            {
                return ERelicType.Armor;
            }
            else if (itemName.Contains("Relic/Helmet"))
            {
                return ERelicType.Helmet;
            }
            else if (itemName.Contains("Relic/Weapon"))
            {
                return ERelicType.Weapon; 
            }

            Debug.LogError($"-- ? no Type {itemName}");
            return ERelicType.Weapon; // 默认返回
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
