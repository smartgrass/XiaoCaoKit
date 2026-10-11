using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 圣器数值模版
    /// </summary>
    public static class RelicValueCalc
    {
        private const float LevelValueRate = 0.3f; //相当于角色等级属性的倍率
        private const float QualityRateBase = 1.2f; //每阶相差倍数

        /// <summary>
        /// 获取圣器数值
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Dictionary<EAttr, float> GetRelicValue(Item item)
        {
            var relicItem = item.ToRelicItem();
            RelicValueModel model = GetRelicValueModel(relicItem.relicType);
            //计算公式
            //属性值 = 属性等级数值 * 类型权重 * 倍率 * 品级倍率
            float qualityRate = ((int)relicItem.quality + 1) * QualityRateBase;
            int level = relicItem.level;
            if (level == 0)
            {
                //防止数值过低
                level = 3;
            }

            Dictionary<EAttr, float> relicValue = new Dictionary<EAttr, float>();
            foreach (var (attr, f) in model.AttrPowerDic)
            {
                float value = GetOriginLevelValue(attr, level) * f * qualityRate;
                relicValue.Add(attr, value);
            }

            return relicValue;
        }

        /// <summary>
        /// 获取原始等级数据
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static float GetOriginLevelValue(EAttr attr, int level)
        {
            float getValue = 0;
            switch (attr)
            {
                case EAttr.Atk:
                    getValue = CharacterAttrCalc.GetBaseATK(level);
                    break;
                case EAttr.Def:
                    getValue = CharacterAttrCalc.GetBaseDEF(level);
                    break;
                case EAttr.MaxHp:
                    getValue = CharacterAttrCalc.GetBaseHP(level);
                    break;
                default:
                    Debug.Log($"-- no attr setting {attr}");
                    break;
            }

            return getValue * LevelValueRate;
        }

        private static RelicValueModel GetRelicValueModel(ERelicType relicType)
        {
            //武器:攻击7+生命3
            // 头盔:攻击3+生命7
            // 盔甲:防御5+生命5
            // 鞋子:攻击3+生命7
            // 饰品:攻击5+生命5
            RelicValueModel model = new RelicValueModel();
            switch (relicType)
            {
                case ERelicType.Weapon:
                    model.AddAttr(EAttr.Atk, 7);
                    model.AddAttr(EAttr.MaxHp, 3);
                    break;
                case ERelicType.Helmet:
                    model.AddAttr(EAttr.Atk, 3);
                    model.AddAttr(EAttr.MaxHp, 7);
                    break;
                case ERelicType.Armor:
                    model.AddAttr(EAttr.Def, 5);
                    model.AddAttr(EAttr.MaxHp, 5);
                    break;
                case ERelicType.Shoe:
                    model.AddAttr(EAttr.Atk, 3);
                    model.AddAttr(EAttr.MaxHp, 7);
                    break;
                case ERelicType.Jewelry:
                    model.AddAttr(EAttr.Atk, 5);
                    model.AddAttr(EAttr.MaxHp, 5);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(relicType), relicType, null);
            }

            return model;
        }
    }

    //数值模版
    public class RelicValueModel
    {
        public Dictionary<EAttr, float> AttrPowerDic = new Dictionary<EAttr, float>();

        private const float ModelValueRate = 0.1f; //权重倍率,默认10等分

        public void AddAttr(EAttr attr, float power)
        {
            AttrPowerDic.Add(attr, power * ModelValueRate);
        }
    }


    /// <summary>
    /// 锻造圣器所需材料数
    /// </summary>
    public static class RelicForgeCostCalc
    {
        //循环段长度(等级)
        public const int CycleLength = 20;

        // 段内等级步长
        public const float StepLength = 10;

        // 每个循环起点基础晶石数量
        public const int BaseCount = 4;

        //段内每一步的数量倍率
        public const float MultiplierStep = 2.0f;

        public const float RankAdd = 0.4f;

        //武器和饰品相对消耗高
        public const float HighRelicTypeAdd = 0.5f;

        public static int CalcForgeCrystal(ERelicType relicType, int level, int quality, out int crystalTier)
        {
            int cycleIndex = level / CycleLength;
            crystalTier = cycleIndex;

            int lvInCycle = level % CycleLength;
            float stepCount = lvInCycle / StepLength;
            float relicTypeAdd = IsHighRelicType(relicType) ? HighRelicTypeAdd : 0;

            double baseAmt = BaseCount * Math.Pow(MultiplierStep, stepCount);
            double final = baseAmt * (1 + RankAdd * quality) * (1 + relicTypeAdd);
            int result = (int)Math.Round(final);
            return Math.Max(result, 1);
        }


        private static bool IsHighRelicType(ERelicType relicType)
        {
            return relicType is ERelicType.Weapon or ERelicType.Jewelry;
        }
        
        //合成所需材料
        public static List<Item> GetNeedItems(RelicItem relicItem)
        {
            List<Item> needItems = new List<Item>();
            int count = CalcForgeCrystal(relicItem.relicType,relicItem.level, (int)relicItem.quality, out int crystalTier);
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