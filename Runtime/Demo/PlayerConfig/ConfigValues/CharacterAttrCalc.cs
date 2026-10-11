using UnityEngine;

public static class CharacterAttrCalc
{
    public const float BaseValueLevel15 = 225f; //15级基准值
    public const float StepMulti = 1.5f; //每10级之间增长n倍
    public const float Level0Value = 50; //0级基准值
    public const int MaxLinearLevel = 15; //前15线性增长

    /// <summary>
    /// 每10级增长StepMulti倍
    /// </summary>
    private static float CalcBy10Step(int lv)
    {
        float k = lv / 10f;
        float val = Mathf.Pow(StepMulti, k);
        return Mathf.Max(val, 1);
    }

    public static float GetBaseATK(int level)
    {
        if (level < MaxLinearLevel)
        {
            return Mathf.Lerp(Level0Value, BaseValueLevel15, (float)level / MaxLinearLevel);
        }

        return CalcBy10Step(level - MaxLinearLevel) * BaseValueLevel15;
    }

    public static float GetBaseHP(int level)
    {
        return GetBaseATK(level) * ATK_HpMult(level);
    }

    //获取 每级升级经验
    public static int GetRoleNextLevelExp(int lv)
    {
        float k = lv / 10f;
        float val = Mathf.Pow(2, k);
        return (int)(75 * val);
    }

    /// <summary>
    /// 获取从1级升到指定等级所需的累计经验
    /// </summary>
    public static int GetTotalExp(int lv)
    {
        int total = 0;
        for (int i = 1; i < lv ; i++)
        {
            total += GetRoleNextLevelExp(i);
        }

        return total;
    }


    public static float ATK_HpMult(int level)
    {
        //默认5倍
        return 5;
    }

    /// <summary>
    /// 裸基础防御力
    /// </summary>
    public static float GetBaseDEF(int level)
    {
        return 7.5f * level;
        const float level15DefValue = 150;
        return level * (level15DefValue / MaxLinearLevel);
    }
}
