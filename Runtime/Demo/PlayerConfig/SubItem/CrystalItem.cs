using System;

namespace XiaoCao
{
    public class CrystalItem : ISubItem
    {
        public string typeId;
        public ECrystalType crystalType;
        public EQuality quality;
        public int count = 1;
        public string LocalizeName => $"Crystal_{crystalType.ToString()}_{(int)quality}";

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