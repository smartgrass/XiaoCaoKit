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

        //合成所需材料
        public static List<Item> GetNeedItems(RelicItem relicItem)
        {
            List<Item> needItems = new List<Item>();
            int count = CalcForgeCrystal(relicItem.level, (int)relicItem.quality, out int crystalTier);
            EQuality crystalQuality = GetCrystalQuality(relicItem.level);
            string id = GetCrystalId(crystalQuality);

            //所需矿石
            needItems.Add(new Item(ItemType.Consumable, id, count, crystalQuality));
            //金币数
            needItems.Add(new Item(ItemType.Coin, nameof(ItemType.Coin), GetNeedCoinCount(relicItem)));
            return needItems;
        }

        //分解产出
        public static List<Item> GetBreakDownItems(RelicItem relicItem)
        {
            var needItems = GetNeedItems(relicItem);
            List<Item> breakDownItems = new List<Item>();
            int addCoin = 0;
            foreach (var needItem in needItems)
            {
                int halfInt = needItem.num / 2;
                if (needItem.type == ItemType.Consumable)
                {
                    float cutOff = needItem.num - halfInt * 2;
                    if (cutOff > 0)
                    {
                        if (needItem.quality != EQuality.White)
                        {
                            var lowerMat = new Item(needItem.type, needItem.typeId, 1, (needItem.quality - 1));
                            breakDownItems.Add(lowerMat);
                        }
                        else
                        {
                            addCoin += 50;
                        }
                    }

                    needItem.num = halfInt;
                    if (halfInt > 0)
                    {
                        breakDownItems.Add(needItem);
                    }
                }
                else if (needItem.type == ItemType.Coin)
                {
                    addCoin += needItem.num / 2;
                }
            }

            if (addCoin > 0)
            {
                breakDownItems.Add(new Item(ItemType.Coin, nameof(ItemType.Coin), addCoin));
            }

            return breakDownItems;
        }


        private static int GetNeedCoinCount(RelicItem relicItem)
        {
            float levelNum = Math.Max(0.5f, relicItem.level / 10);
            return (int)(100 * levelNum * GetQualityNumRate(relicItem.quality));
        }


        private static float GetQualityNumRate(EQuality quality)
        {
            //1->1.25->1.5
            return 1 + (int)quality * 0.25f;
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