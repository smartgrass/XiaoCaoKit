using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace XiaoCao
{
    ///定位<see cref="Inventory"/>
    namespace XCItem 
    {
    }

    // 物品类，包含类型、数量和名字属性, 用于存档, 字段经过压缩
    [System.Serializable]
    public class Item
    {
        public ItemType type;
        public string typeId; //物品信息 
        public int num; // 数量/等级
        public EQuality quality;

        public int InstanceId
        {
            get => num;
            set => num = value;
        }
        
        //真正的存储的id
        public string ItemKey
        {
            get
            {
                if (type == ItemType.Coin)
                {
                    return type.ToString();
                }
                //TODO 装备不可堆叠+身份识别问题
                return $"{typeId}";
            }
        }

        public Item(ItemType itemType, string itemTypeId, int numInfo = 1, EQuality itemQuality = EQuality.White)
        {
            typeId = itemTypeId;
            type = itemType;
            num = numInfo;
            quality = itemQuality;
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

    /// SubItem 需要配置
    /// <see cref="SpriteResHelper.GetItemSprite"/>
    /// <see cref="LocalizeKey.GetItemName"/>
    public interface ISubItem
    {
        public Item ToItem();
    }

    public static class ItemExtend
    {
        public static BuffItem ToBuffItem(this Item item)
        {
            return BuffItem.Create(item);
        }

        public static CrystalItem ToCrystalItem(this Item item)
        {
            return CrystalItem.Create(item.typeId, item.num);
        }
        
        public static BattleExtraItemData ToBattleExtraItemData(this Item item)
        {
            BattleExtraItemSubConfig config = ConfigMgr.Inst.BattleExtraItemConfigSo?.GetConfig(item.typeId);
            if (config == null)
            {
                Debug.LogWarning($"--- no extra item config {item.typeId}");
                return null;
            }

            return BattleExtraItemData.Create(item, config, item.num);
        }

        public static RelicItem ToRelicItem(this Item item)
        {
            return RelicItem.CreateByItem(item);
        }

        /// <summary>
        /// 结算 OtherValue 类道具: 这类道具不是实际物品, 不进入背包, 直接作用到角色身上
        /// </summary>
        public static void RewardOtherValue(this Item item)
        {
            switch (item.typeId)
            {
                case OtherValueItemId.Exp:
                    PlayerSaveData.LocalSavaData.AddExp(item.num);
                    break;
                default:
                    Debug.LogWarning($"--- no reward handle, OtherValue typeId: {item.typeId}");
                    break;
            }
        }

        /// <summary>
        /// 合并可堆叠物品, 如金币,矿石; 可堆叠物品按物品键累加数量, 并保持首次出现的顺序
        /// 圣器不可堆叠, Buff 无需处理, 两者原样保留
        /// </summary>
        /// <returns>合并后的新列表, 不修改传入的列表</returns>
        public static List<Item> CombineList(this List<Item> list)
        {
            List<Item> result = new List<Item>();
            Dictionary<string, Item> combineDic = new Dictionary<string, Item>();

            foreach (Item item in list)
            {
                if (!IsCombineAbleType(item.type))
                {
                    //圣器, Buff 不合并
                    result.Add(new Item(item.type, item.typeId, item.num, item.quality));
                    continue;
                }

                if (combineDic.TryGetValue(item.ItemKey, out Item combineItem))
                {
                    combineItem.num += item.num;
                }
                else
                {
                    Item newItem = new Item(item.type, item.typeId, item.num, item.quality);
                    combineDic.Add(item.ItemKey, newItem);
                    result.Add(newItem);
                }
            }

            return result;
        }

        /// <summary>
        /// 是否可合并: 圣器不可堆叠, Buff 由各自系统处理
        /// </summary>
        private static bool IsCombineAbleType(ItemType type)
        {
            return type != ItemType.HolyRelic && type != ItemType.Buff;
        }
        
    }
}
