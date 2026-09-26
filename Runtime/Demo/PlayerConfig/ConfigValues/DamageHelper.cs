using UnityEngine;

/// <summary>
/// 除法比例型RPG伤害计算
/// </summary>
public class DamageHelper
{
    //K：防御常数（平衡参数，自行调整，推荐 1000/1500/2000） 
    private const float K = 1000f; // 全局固定防御常数

    //攻击,防御,穿透
    public static float CalcDamageAtkInfo(AtkInfo atkInfo, int def, float defPenetrationPercent = 0)
    {
        float effecDamage = DamageHelper.CalcDamage(atkInfo.atk,
            def, atkInfo.isCrit, 1.75f, defPenetrationPercent);
        return effecDamage;
    }


    /// <param name="effectiveAtk">有效攻击力</param>
    /// <param name="targetDef">目标面板防御力</param>
    /// <param name="defPenetrationPercent">防御穿透百分比 0~1 (0.3=30%穿透)</param>
    /// <param name="isCrit">是否暴击</param>
    /// <param name="critDmgMultiplier">暴击倍率，2.0=200%爆伤</param>
    /// <param name="damageFloor">伤害保底，建议1</param>
    /// <returns>最终伤害值</returns>
    public static float CalcDamage(
        float effectiveAtk,
        int targetDef,
        bool isCrit,
        float critDmgMultiplier,
        float defPenetrationPercent = 0)
    {
        // 有效防御，不能小于0
        float effectiveDEF = Mathf.Max(targetDef * (1f - defPenetrationPercent), 0f);

        // 基础攻防伤害
        float baseDmg = effectiveAtk * (K / (effectiveDEF + K));

        // 暴击乘区
        float critMult = isCrit ? critDmgMultiplier : 1f;
        float afterCritDmg = baseDmg * critMult;

        // 保底伤害1
        float finalDmg = Mathf.Max(afterCritDmg, 1);

        return finalDmg;
    }
}