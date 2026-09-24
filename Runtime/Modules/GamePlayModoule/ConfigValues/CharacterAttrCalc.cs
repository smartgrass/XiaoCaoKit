using UnityEngine;

public static class CharacterAttrCalc
{
    /// <summary>
    /// 裸基础攻击力（二次多项式，初始高、增速缓）
    /// 锚点：Lv1=30，Lv10=150，Lv25=500，Lv50=1500
    /// </summary>
    public static float GetBaseATK(int level)
    {
        float a = 0.4167f;
        float b = 8.75f;
        float c = 20.833f;
        return a * level * level + b * level + c;
    }


    /// <summary>
    /// 裸基础防御力 二次多项式 Lv10=10，Lv30=133
    /// </summary>
    public static float GetBaseDEF(int level)
    {
        float a = 0.4144f;
        float b = -1.5544f;
        float c = 4.1400f;
        return a * level * level + b * level + c;
    }

    /// <summary>
    /// 裸基础生命值：Lv1~9线性，Lv10+二次多项式
    /// </summary>
    public static float GetBaseHP(int level)
    {
        float baseHp = 160;
        if (level < 1) return 0;
        if (level < 10)
        {
            return baseHp + 190f * (level - 1f) / 9f;
        }

        float a = 2.1875f;
        float b = -29.375f;
        float c = 261.875f + baseHp;
        return a * level * level + b * level + c;
    }
}