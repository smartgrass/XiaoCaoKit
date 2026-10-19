using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using cfg;
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
            //OtherValue 不是实际物品, 直接结算到角色身上, 不进入背包
            if (itemType == ItemType.OtherValue)
            {
                new Item(itemType, itemName, itemCount).RewardOtherValue();
                return;
            }

            Item findItem = FindItemByName(itemName);
            if (findItem != null)
            {
                findItem.num += itemCount;
                Debug.Log("Added " + itemCount + " to inventory. Count: " + findItem.num);
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

            //OtherValue 不是实际物品, 直接结算到角色身上, 不进入背包
            if (item.type == ItemType.OtherValue)
            {
                item.RewardOtherValue();
                return;
            }

            if (IsNumaAbleType(item.type))
            {
                //可堆叠 直接增加数量
                Item findItem = items.Find(inventoryItem =>
                    inventoryItem != null && inventoryItem.ItemKey == item.ItemKey);
                if (findItem != null)
                {
                    findItem.num += item.num;
                    return;
                }
            }


            items.Add(new Item(item.type, item.typeId, item.num, item.quality));
        }

        ////是否可堆叠
        private bool IsNumaAbleType(ItemType type)
        {
            return type != ItemType.HolyRelic;
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
            //消耗为0(免费)时不需要持有该物品
            if (amount <= 0)
            {
                return true;
            }

            Item item = FindItemByName(itemName);
            return item != null && item.num >= amount;
        }


        public bool CheckEnoughItemList(List<Item> list, bool isConsumeItem)
        {
            if (list == null)
            {
                return true;
            }

            foreach (var item in list)
            {
                if (item == null || item.num <= 0)
                {
                    //免费项不参与校验, 避免背包里没有该物品时被判定为材料不足
                    continue;
                }

                if (!CheckEnoughItem(item.ItemKey, item.num))
                {
                    return false;
                }
            }

            if (isConsumeItem)
            {
                foreach (var item in list)
                {
                    if (item == null || item.num <= 0)
                    {
                        continue;
                    }

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
                var h = i.ToRelicItem();
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
            return equippedHolyRelics.Select(i => i.ToRelicItem()).ToList();
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
        ExtraItem = 4, //关卡内可以使用的道具
        OtherValue  = 5, //不进入背包,非实际物品,如经验值
    }

    /// <summary>
    /// <see cref="ItemType.OtherValue"/> 道具的 typeId 约定
    /// </summary>
    public static class OtherValueItemId
    {
        public const string Exp = "Exp"; //经验值, 直接加在角色身上
    }

    public enum EConsumableSubType
    {
        None,
        Crystal
    }

    public struct AddAttr
    {
        public EAttr EAttr;
        public AttributeModifier modifier;
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
