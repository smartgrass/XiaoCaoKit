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
        private const float ModelValueRate = 0.1f; //权重倍率
        private const float QualityRateBase = 1.2f; //每阶相差倍数


        /// <summary>
        /// 获取圣器数值
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Dictionary<EAttr, float> GetRelicValue(Item item)
        {
            Dictionary<EAttr, float> relicValue = new Dictionary<EAttr, float>();
            var relicItem = item.ToRelicItem();
            var model = GetRelicValueModel(relicItem.relicType, relicItem.quality);
            //计算公式
            //属性值 = 属性等级数值 * 类型权重 * 倍率 * 品级倍率
            float qualityRate = ((int)relicItem.quality + 1) * QualityRateBase;
            int level = relicItem.level;
            if (level == 0)
            {
                //防止数值过低
                level = 3;
            }
            
            foreach (var (attr, f) in model.attrPowerDic)
            {
                float value = GetLevelValue(attr, level) * f * ModelValueRate * qualityRate;
                relicValue.Add(attr, value);
            }

            return relicValue;
        }

        private static float GetLevelValue(EAttr attr, int level)
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

        private static RelicValueModel GetRelicValueModel(ERelicType relicType, EQuality quality)
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
        public Dictionary<EAttr, float> attrPowerDic = new Dictionary<EAttr, float>();

        public void AddAttr(EAttr attr, float power)
        {
            attrPowerDic.Add(attr, power);
        }
    }
}