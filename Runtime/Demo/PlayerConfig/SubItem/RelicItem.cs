using System;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao.UI;

namespace XiaoCao
{
    public struct RelicItem : ISubItem
    {
        public ERelicType relicType;
        public EQuality quality;
        public string typeId;
        public int level; //圣器等级

        public string LocalizeName => 
        $"{GetBaseName()}(Lv{level})";

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

        public string GetBaseName()
        {
            if (LocalizeMgr.Inst.HasKey(typeId))
            {
                return typeId.ToLocalizeStr();
            }

            string key = $"Relic/{relicType}_{(int)quality}";
            return key.ToLocalizeStr();
        }
    }
}