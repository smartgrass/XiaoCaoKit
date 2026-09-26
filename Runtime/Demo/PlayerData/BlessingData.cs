using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 祝福属性类型。
    /// </summary>
    public enum EBlessing
    {
        Hp,
        Atk,
        Crit,
        Def
    }

    /// <summary>
    /// 祝福碎片的等级、消耗和属性收益规则。
    /// </summary>
    public static class BlessingRule
    {
        public const int MaxLevel = 30;

        public static readonly EBlessing[] AllBlessings =
        {
            EBlessing.Hp,
            EBlessing.Atk,
            EBlessing.Crit,
            EBlessing.Def
        };

        private const float CostLinearFactor = 0.12f;
        private const float CostSquareFactor = 0.018f;
        private const int CostStageAdd = 10;

        /// <summary>
        /// 获取祝福显示名称。
        /// </summary>
        public static string GetName(EBlessing blessing)
        {
            return LocalizeMgr.Localize(LocalizeKey.GetBlessingNameKey(blessing));
        }

        /// <summary>
        /// 获取祝福碎片显示名称。
        /// </summary>
        public static string GetFragmentName(EBlessing blessing)
        {
            return LocalizeMgr.Localize(GetFragmentTypeId(blessing));
        }

        /// <summary>
        /// 获取祝福碎片物品类型 ID。
        /// </summary>
        public static string GetFragmentTypeId(EBlessing blessing)
        {
            return blessing switch
            {
                EBlessing.Hp => "Blessing_Hp_Fragment",
                EBlessing.Atk => "Blessing_Atk_Fragment",
                EBlessing.Crit => "Blessing_Crit_Fragment",
                EBlessing.Def => "Blessing_Def_Fragment",
                _ => "Blessing_Fragment"
            };
        }

        /// <summary>
        /// 获取背包中用于消耗的祝福碎片键。
        /// </summary>
        public static string GetFragmentItemKey(EBlessing blessing)
        {
            return CreateFragmentItem(blessing, 1).ItemKey;
        }

        /// <summary>
        /// 创建祝福碎片物品数据。
        /// </summary>
        public static Item CreateFragmentItem(EBlessing blessing, int count)
        {
            return new Item(ItemType.Consumable, GetFragmentTypeId(blessing), count);
        }

        /// <summary>
        /// 获取升到目标等级所需的碎片数量。
        /// </summary>
        public static int GetUpgradeCost(EBlessing blessing, int targetLevel)
        {
            if (targetLevel > MaxLevel)
            {
                return 0;
            }

            int level = Mathf.Max(1, targetLevel);
            int baseCost = GetBaseCost(blessing);
            int levelOffset = level - 1;
            float cost = baseCost * (1 + CostLinearFactor * levelOffset + CostSquareFactor * levelOffset * levelOffset);
            return Mathf.CeilToInt(cost) + CostStageAdd * (levelOffset / 5);
        }

        /// <summary>
        /// 获取升到指定等级的累计碎片需求。
        /// </summary>
        public static int GetTotalCost(EBlessing blessing, int targetLevel)
        {
            int level = Mathf.Clamp(targetLevel, 0, MaxLevel);
            int total = 0;
            for (int i = 1; i <= level; i++)
            {
                total += GetUpgradeCost(blessing, i);
            }

            return total;
        }

        /// <summary>
        /// 判断祝福是否已经达到等级上限。
        /// </summary>
        public static bool IsMaxLevel(int level)
        {
            return level >= MaxLevel;
        }

        /// <summary>
        /// 获取祝福的主属性枚举。
        /// </summary>
        public static EAttr GetPrimaryAttr(EBlessing blessing)
        {
            return blessing switch
            {
                EBlessing.Hp => EAttr.MaxHp,
                EBlessing.Atk => EAttr.Atk,
                EBlessing.Crit => EAttr.Crit,
                EBlessing.Def => EAttr.Def,
                _ => EAttr.MaxHp
            };
        }

        /// <summary>
        /// 获取祝福对主属性的修正值。
        /// </summary>
        public static AttributeModifier GetPrimaryModifier(EBlessing blessing, int level)
        {
            int safeLevel = Mathf.Clamp(level, 0, MaxLevel);
            int breakLevel = GetBreakLevel(safeLevel);
            return blessing switch
            {
                EBlessing.Hp => new AttributeModifier { Add = safeLevel * 10, Multiply = breakLevel * 0.01f },
                EBlessing.Atk => new AttributeModifier { Add = safeLevel, Multiply = breakLevel * 0.01f },
                EBlessing.Crit => new AttributeModifier { Add = GetCritRateBonus(safeLevel) },
                EBlessing.Def => new AttributeModifier { Add = safeLevel + breakLevel },
                _ => default
            };
        }

        /// <summary>
        /// 获取暴击祝福提供的额外暴击伤害。
        /// </summary>
        public static float GetCritDamageBonus(int level)
        {
            return GetBreakLevel(Mathf.Clamp(level, 0, MaxLevel)) * 0.01f;
        }

        /// <summary>
        /// 获取祝福当前等级的效果描述。
        /// </summary>
        public static string GetEffectText(EBlessing blessing, int level)
        {
            int safeLevel = Mathf.Clamp(level, 0, MaxLevel);
            int breakLevel = GetBreakLevel(safeLevel);
            return blessing switch
            {
                EBlessing.Hp => JoinEffectLines(
                    GetAddLine("Hp", safeLevel * 10),
                    GetPercentLine("Hp", breakLevel)),
                EBlessing.Atk => JoinEffectLines(
                    GetAddLine(EAttr.Atk.ToString(), safeLevel),
                    GetPercentLine(EAttr.Atk.ToString(), breakLevel)),
                EBlessing.Crit => JoinEffectLines(
                    GetPercentLine(EAttr.Crit.ToString(), GetCritRateBonus(safeLevel)),
                    GetPercentLine(EAttr.CritDamage.ToString(), GetCritDamageBonus(safeLevel) * 100f)),
                EBlessing.Def => GetAddLine(EAttr.Def.ToString(), safeLevel + breakLevel),
                _ => ""
            };
        }

        /// <summary>
        /// 获取祝福当前等级的效果描述。
        /// </summary>
        public static string GetLevelEffectText(EBlessing blessing, int currentLevel)
        {
            int level = Mathf.Clamp(currentLevel, 0, MaxLevel);
            return GetEffectText(blessing, level);
        }

        /// <summary>
        /// 获取属性修正使用的唯一键。
        /// </summary>
        public static string GetModifierKey(EBlessing blessing)
        {
            return $"Blessing_{blessing}";
        }

        /// <summary>
        /// 获取暴击伤害修正使用的唯一键。
        /// </summary>
        public static string GetCritDamageModifierKey()
        {
            return "Blessing_CritDamage";
        }

        /// <summary>
        /// 获取祝福消耗的基础系数。
        /// </summary>
        private static int GetBaseCost(EBlessing blessing)
        {
            return blessing == EBlessing.Crit ? 15 : 10;
        }

        private static int GetBreakLevel(int level)
        {
            return Mathf.Max(0, level) / 5;
        }

        private static float GetCritRateBonus(int level)
        {
            int breakLevel = GetBreakLevel(level);
            return level * 0.25f + breakLevel * 0.5f;
        }

        private static string GetAddLine(string attrKey, int value)
        {
            return $"{LocalizeMgr.Localize(attrKey)} +{value}";
        }

        private static string GetPercentLine(string attrKey, float value)
        {
            return $"{LocalizeMgr.Localize(attrKey)} +{value:0.##}%";
        }

        private static string JoinEffectLines(params string[] lines)
        {
            string result = "";
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                result = string.IsNullOrEmpty(result) ? line : $"{result}\n{line}";
            }

            return result;
        }
    }
}
