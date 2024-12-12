using UnityEngine;

namespace XiaoCao
{
    public enum EBuff
    {
        None = 0,

        [InspectorName("击杀回血3% HP")]
        KillRecoverHp,

        [InspectorName("吸血1%")]
        AtkRecoverHp,

        [InspectorName("增加攻击力2.5%")]
        AtkAdd,

        [InspectorName("主动技能伤害5%")]
        SkillDamageAdd,

        [InspectorName("最大生命5%")]
        MaxHpAdd,

        [InspectorName("暴击率提升2.5%")]
        CritAdd,

        [InspectorName("技能冷缩 5%")]
        SkillCDOff,

        [InspectorName("移速增加 5%")]
        MovementSpeedAdd,

        [InspectorName("攻速提升5%")]
        AttackSpeedAdd,

        [InspectorName("大招能量恢复 20%")]
        UltimateEnergyRestore,

        [InspectorName("受伤时生成最大生命值5%伤害的护盾,持续5s, cd5秒。")]
        ShieldOnDamage,

        [InspectorName("生命值低于50%时, 攻击力提升8%")]
        AtkAddIfBelowHalfHp
    }

    public static class BuffSetting
    {
        public static EBuffType GetBuffType(this EBuff buff)
        {
            switch (buff)
            {
                case EBuff.KillRecoverHp:
                case EBuff.AtkRecoverHp:
                case EBuff.MaxHpAdd:
                case EBuff.ShieldOnDamage:
                    return EBuffType.Def;

                case EBuff.AtkAdd:
                case EBuff.SkillDamageAdd:
                case EBuff.CritAdd:
                case EBuff.AtkAddIfBelowHalfHp:
                    return EBuffType.Atk;
                case EBuff.SkillCDOff:
                case EBuff.MovementSpeedAdd:
                case EBuff.AttackSpeedAdd:
                case EBuff.UltimateEnergyRestore:
                    return EBuffType.Other;
                default:
                    return EBuffType.Atk;
            }
        }
    }


    //Buff 分类
    public enum EBuffType
    {
        Def,
        Atk,
        Other
    }
}
