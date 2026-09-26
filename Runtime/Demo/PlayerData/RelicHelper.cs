using cfg;
using UnityEngine;

namespace XiaoCao
{
    [XCHelper]
    public static class RelicHelper
    {
        public static RelicItem GetRelicItem(this ForgeRelicSetting configRow)
        {
            return RelicItem.Create(configRow.Id);
        }

        public static bool IsRelicUnlock(this ForgeRelicSetting configRow)
        {
            if (string.IsNullOrEmpty(configRow.UnlockedCondition))
            {
                return true;
            }

            return UnlockDataHelper.CheckUnlock(configRow.UnlockedCondition);
        }

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
}