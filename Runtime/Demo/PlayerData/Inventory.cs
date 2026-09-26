using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using cfg;
using UnityEngine;
using UnityEngine.Serialization;
using XiaoCao.UI;

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
    }

    public struct RelicItem : ISubItem
    {
        public ERelicType relicType;
        public EQuality quality;
        public string typeId;
        public int level; //圣器等级

        public string LocalizeName => GetName();

        public Color GetColor() => quality.GetQualityColor();

        public List<AddAttr> AddAttr { get; set; }

        public Item ToItem()
        {
            Item item = new Item(ItemType.HolyRelic, typeId, level, quality);
            return item;
        }

        public static RelicItem CreateByItem(Item item)
        {
            return Create(item.typeId);
        }

        public static Item CreateItem(string typeId)
        {
            return Create(typeId).ToItem();
        }

        //示例: Relic/Weapon_1_10
        public static RelicItem Create(string typeId)
        {
            RelicItem relic = new RelicItem();
            string[] strArray = typeId.Split('_');
            if (strArray.Length < 3)
            {
                throw new Exception($"Invalid relic {typeId}");
            }

            string relicTypeName = strArray[0].Replace("Relic/", string.Empty);
            Enum.TryParse<ERelicType>(relicTypeName, out var relicType);

            var quality = (EQuality)int.Parse(strArray[1]);

            relic.typeId = typeId;
            relic.relicType = relicType;
            relic.quality = quality;
            relic.level = int.Parse(strArray[2]);

            return relic;
        }

        private string GetName()
        {
            if (LocalizeMgr.Inst.HasKey(typeId))
            {
                return typeId.ToLocalizeStr();
            }

            string key = $"Relic/{relicType}_{(int)quality}";
            return key.ToLocalizeStr();
        }
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