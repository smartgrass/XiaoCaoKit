using System;
using XiaoCao.UI;

namespace XiaoCao
{
    public class CrystalItem : ISubItem
    {
        public string typeId;
        public ECrystalType crystalType;
        public EQuality quality;
        public int count = 1;

        /// <summary>基础名称的本地化key(不含品阶), 例: Crystal_B</summary>
        public string LocalizeNameKey => $"Crystal_{crystalType}";

        /// <summary>带品阶后缀的名称, 例: Crystal_B_1 -> 深渊结晶(中级)</summary>
        public string LocalizeName => $"{LocalizeNameKey.ToLocalizeStr()}{GetLevelSuffix()}";

        /// <summary>
        /// 描述本地化key: 先找带品阶的 Crystal_B_Desc_1, 找不到再退回 Crystal_B_Desc
        /// </summary>
        public string LocalizeDescKey
        {
            get
            {
                string levelKey = $"{LocalizeNameKey}_Desc_{(int)quality}";
                return LocalizeMgr.Inst.HasKey(levelKey) ? levelKey : $"{LocalizeNameKey}_Desc";
            }
        }

        /// <summary>矿石描述文案</summary>
        public string LocalizeDesc => LocalizeDescKey.ToLocalizeStr();

        /// <summary>
        /// 品阶后缀, 取 OreLevel/[品阶], 例: (中级); 未配置对应品阶时返回空串
        /// </summary>
        private string GetLevelSuffix()
        {
            string key = $"OreLevel/{(int)quality}";
            return LocalizeMgr.Inst.HasKey(key) ? $"({key.ToLocalizeStr()})" : string.Empty;
        }

        public Item ToItem()
        {
            return new Item(ItemType.Consumable, typeId, count, quality);
        }


        //Crystal_A_0
        public static CrystalItem Create(string typeId, int count = 1)
        {
            var cry = new CrystalItem();

            string[] strArray = typeId.Split('_');
            if (strArray.Length < 3)
            {
                throw new Exception($"Invalid relic {typeId}");
            }

            Enum.TryParse<ECrystalType>(strArray[1], out var relicType);

            var quality = (EQuality)int.Parse(strArray[2]);
            cry.typeId = typeId;
            cry.quality = quality;
            cry.crystalType = relicType;
            return cry;
        }
    }

    public enum ECrystalType
    {
        A,
        B,
        C,
    }
}
