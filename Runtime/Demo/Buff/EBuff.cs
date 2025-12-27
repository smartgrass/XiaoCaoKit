using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 序号需要保持不变
    /// </summary>
    public enum EBuff
    {
        None = 0,

        [InspectorName("击杀回血3% HP")] KillRecoverHpMult,

        [InspectorName("吸血1%")] AtkRecoverHpMult,

        [InspectorName("增加攻击力2.5%")] AtkMult,
        [InspectorName("最大生命5%")] MaxHpMult,

        [InspectorName("暴击率提升2.5%")] CritAdd,

        [InspectorName("技能冷缩 5%")] SkillCDOff,

        [InspectorName("移速增加 5%")] MoveSpeedMultAdd,

        [InspectorName("大招能量恢复 20%")] UltimateEnergyRestore,

        //相对的 有大招伤害
        [InspectorName("主动技能伤害5%")] SkillDamageMult,

        [InspectorName("普攻速度增加15%")] NorAtkSpeedAdd,

        [InspectorName("exBuff分界线")] NorBuffEndIndex = 100,

        [InspectorName("自动召唤魔法导弹,cd{0:0.#}s")] MagicMissile = 101,

        [InspectorName("使用剑刃攻击时,产生额外剑气,每10次攻击,前{0:0}次触发")] ExtraSlash = 102,

        [InspectorName("攻击敌人时触发闪电传递,造成{0:P0}的伤害,冷缩{1:F2}")] ThunderExplode = 103,


        //小分支: 防御系纹章>=200
        [InspectorName("受到攻击时,回复{0}%最大生命,cd{1:0.#}s")]
        HurtRecover = 200,
        // [InspectorName("闪避成功,使周围目标陷入时停3s")] LimitDash
    }

    public static class BuffSetting
    {
        public static EBuffType GetBuffType(this EBuff buff)
        {
            if (buff == EBuff.None)
            {
                return EBuffType.None;
            }

            int index = (int)buff;
            if (index < (int)EBuff.NorBuffEndIndex)
            {
                return EBuffType.Nor;
            }
            else if (index == (int)EBuff.NorBuffEndIndex)
            {
                return EBuffType.None;
            }

            return EBuffType.Ex;
        }
    }


    //Buff 分类
    public enum EBuffType
    {
        None = -1,
        Nor = 0,
        Ex = 2,
        Revive = 3
    }
}