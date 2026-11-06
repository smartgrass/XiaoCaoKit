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

        //不可堆叠物品(圣器)的实例 id: 同名同等级的多件是不同物品, 靠它区分; 0 表示旧存档尚未分配
        public int instanceId;

        //进程内自增的实例 id 分配器
        private static int _nextInstanceId = 1;

        /// <summary>
        /// 物品实例的唯一 id, 未分配时自动分配。
        /// </summary>
        public int InstanceId => EnsureInstanceId();

        /// <summary>
        /// 确保当前物品已分配实例 id, 并让分配器跳过存档里已有的 id; 兼容没有该字段的旧存档。
        /// </summary>
        public int EnsureInstanceId()
        {
            if (instanceId > 0)
            {
                _nextInstanceId = Mathf.Max(_nextInstanceId, instanceId + 1);
                return instanceId;
            }

            instanceId = _nextInstanceId++;
            return instanceId;
        }

        /// <summary>
        /// 是否为同一件物品: 有实例 id 时按 id 比较, 否则退回引用比较。
        /// </summary>
        public bool IsSameInstance(Item other)
        {
            if (other == null)
            {
                return false;
            }

            return ReferenceEquals(this, other) || (instanceId > 0 && instanceId == other.instanceId);
        }

        /// <summary>
        /// 复制一份物品数据并保留实例 id: 在背包与装备位之间搬运时不产生新身份。
        /// </summary>
        public Item Clone()
        {
            Item copy = new Item(type, typeId, num, quality);
            copy.instanceId = InstanceId;
            return copy;
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

                //圣器等不可堆叠物品的键只到 typeId, 多件同名同等级物品的身份由 InstanceId 区分
                return $"{typeId}";
            }
        }

        public Item(ItemType itemType, string itemTypeId, int numInfo = 1, EQuality itemQuality = EQuality.White)
        {
            typeId = itemTypeId;
            type = itemType;
            num = numInfo;
            quality = itemQuality;
            //每件物品创建时即分配实例 id, 同名圣器不会互相顶替
            instanceId = _nextInstanceId++;
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
        /// 结算 OtherValue 类道具: 这类道具不是实际物品, 不进入背包, 直接作用到角色身上。
        /// 具体结算规则见 <see cref="PlayerSaveData.AddOtherValue"/>
        /// </summary>
        public static void RewardOtherValue(this Item item)
        {
            PlayerSaveData.LocalSavaData.AddOtherValue(item.typeId, item.num);
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
                if (!item.type.IsStackable())
                {
                    //圣器, Buff 不合并
                    result.Add(item.Clone());
                    continue;
                }

                if (combineDic.TryGetValue(item.ItemKey, out Item combineItem))
                {
                    combineItem.num += item.num;
                }
                else
                {
                    Item newItem = item.Clone();
                    combineDic.Add(item.ItemKey, newItem);
                    result.Add(newItem);
                }
            }

            return result;
        }

        /// <summary>
        /// 物品类型是否可堆叠: 圣器的 num 是等级、Buff 由各自系统处理, 两者都不堆叠, 其余按数量累加。
        /// UI 上不可堆叠的物品不显示数量文本
        /// </summary>
        public static bool IsStackable(this ItemType type)
        {
            return type != ItemType.HolyRelic && type != ItemType.Buff;
        }
        
    }
}
