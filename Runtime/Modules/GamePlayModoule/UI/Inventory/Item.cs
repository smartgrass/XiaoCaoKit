using System.Text.RegularExpressions;

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

        //真正的存储的id
        public string ItemKey
        {
            get
            {
                if (type == ItemType.Coin)
                {
                    return type.ToString();
                }

                return $"{typeId}_{quality}";
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

        public static HolyRelicItem ToHolyRelicItem(this Item item)
        {
            return HolyRelicItem.Create(item);
        }
    }
}