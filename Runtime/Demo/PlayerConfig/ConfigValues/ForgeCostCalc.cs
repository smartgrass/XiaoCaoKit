using System;
using System.Collections.Generic;

namespace XiaoCao
{
    /// <summary>
    /// 锻造圣器所需材料数
    /// </summary>
    public static class ForgeCostCalc
    {
        //循环段长度(等级)
        public const int CycleLength = 20;

        // 段内等级步长
        public const int StepLength = 10;

        // 每个循环起点基础晶石数量
        public const int BaseCount = 4;

        //段内每一步的数量倍率
        public const float MultiplierStep = 2.0f;

        public const float RankAdd = 0.4f;

        public static int CalcForgeCrystal(int level, int quality, out int crystalTier)
        {
            int cycleIndex = level / CycleLength;
            crystalTier = cycleIndex;

            int lvInCycle = level % CycleLength;
            int stepCount = lvInCycle / StepLength;

            double baseAmt = BaseCount * Math.Pow(MultiplierStep, stepCount);
            double final = baseAmt * (1 + RankAdd * quality);
            int result = (int)Math.Round(final);
            return Math.Max(result, 1);
        }

        public static List<Item> GetNeedItems(RelicItem relicItem)
        {
            List<Item> needItems = new List<Item>();
            int count = CalcForgeCrystal(relicItem.level, (int)relicItem.quality, out int crystalTier);
            EQuality crystalQuality = GetCrystalQuality(relicItem.level);
            string id = GetCrystalId(crystalQuality);
            needItems.Add(new Item(ItemType.Consumable, id, count, crystalQuality));
            return needItems;
        }

        private static EQuality GetCrystalQuality(int level)
        {
            int lvInCycle = level / CycleLength;
            return (EQuality)lvInCycle;
        }

        private static string GetCrystalId(EQuality quality)
        {
            return $"Crystal_B_{(int)quality}";
        }
    }
}