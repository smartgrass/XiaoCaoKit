using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 支援角色数值设定, 规则见 Assets/_Md/系统设计/支援角色设定.md
    /// 等级与玩家当前等级一致; 初始状态: 继承玩家同等级基础数据(不含祝福/纹章加成) 的 30% 攻击 / 50% 血量, 解锁 1 技能
    /// 养成提升(每项 Lv1~Lv5):
    /// - 攻击: 30% -> 80% (线性)
    /// - 生存: 50% -> 100% (线性)
    /// - 技巧: 每级解锁 1 个技能(上限 4 个), 满级后支援使用的技能等级由 1 提升至 2
    /// 三项养成等级存于 <see cref="PlayerSaveData"/>(supportAtkLv/supportHpLv/supportSkillLv),
    /// 升级方式和浏览界面目前暂未实装, 可通过 PlayerSaveDataWindow 调试修改
    /// </summary>
    public static class SupportRoleSetting
    {
        public enum SupportGrowType
        {
            Atk,
            Hp,
            Skill,
        }

        public const int DefaultGrowLevel = 1;

        public const int MaxGrowLevel = 5;

        // 攻击继承比例区间: Lv1 30% -> Lv5 80%
        public const float AtkRateMin = 0.30f;
        public const float AtkRateMax = 0.80f;

        // 生存(血量)继承比例区间: Lv1 50% -> Lv5 100%
        public const float HpRateMin = 0.50f;
        public const float HpRateMax = 1.00f;

        // 技巧最多解锁的技能数
        public const int MaxUnlockSkillCount = 4;

        /// <summary>攻击养成等级, 读存档; 无存档上下文(编辑器等)时为初始值</summary>
        public static int GetAtkGrowLevel()
        {
            return GetGrowLevel(SupportGrowType.Atk);
        }

        /// <summary>生存养成等级, 读存档; 无存档上下文(编辑器等)时为初始值</summary>
        public static int GetHpGrowLevel()
        {
            return GetGrowLevel(SupportGrowType.Hp);
        }

        /// <summary>技巧养成等级, 读存档; 无存档上下文(编辑器等)时为初始值</summary>
        public static int GetSkillGrowLevel()
        {
            return GetGrowLevel(SupportGrowType.Skill);
        }

        /// <summary>
        /// 统一读取养成等级: 没有存档上下文时回退到初始等级。
        /// </summary>
        public static int GetGrowLevel(SupportGrowType kind)
        {
            PlayerSaveData saveData = PlayerSaveData.LocalSavaData;
            if (saveData == null)
            {
                return DefaultGrowLevel;
            }
            return saveData.GetSupportGrowLevel(kind);
        }

        /// <summary>
        /// 攻击继承比例: 玩家无等级默认攻击 * 该比例 = 支援角色攻击
        /// </summary>
        public static float GetAtkRate()
        {
            return GetAtkRate(GetAtkGrowLevel());
        }

        /// <summary>
        /// 血量继承比例: 玩家无等级默认血量 * 该比例 = 支援角色血量
        /// </summary>
        public static float GetHpRate()
        {
            return GetHpRate(GetHpGrowLevel());
        }

        /// <summary>
        /// 支援角色可使用的技能数(按玩家已学会技能的 id 顺序取前 N 个)
        /// </summary>
        public static int GetUnlockSkillCount()
        {
            return GetUnlockSkillCount(GetSkillGrowLevel());
        }

        /// <summary>
        /// 支援角色使用玩家技能时的等级: 初始固定 1 级, 技巧满级后提升为 2 级
        /// </summary>
        public static int GetSkillLevelAdd()
        {
            return GetSkillLevelAdd(GetSkillGrowLevel());
        }

        public static float GetAtkRate(int growLevel)
        {
            float t = Mathf.Clamp01((ClampLevel(growLevel) - 1) / (float)(MaxGrowLevel - 1));
            return Mathf.Lerp(AtkRateMin, AtkRateMax, t);
        }

        public static float GetHpRate(int growLevel)
        {
            float t = Mathf.Clamp01((ClampLevel(growLevel) - 1) / (float)(MaxGrowLevel - 1));
            return Mathf.Lerp(HpRateMin, HpRateMax, t);
        }

        public static int GetUnlockSkillCount(int growLevel)
        {
            return Mathf.Clamp(growLevel, 1, MaxUnlockSkillCount);
        }

        public static int GetSkillLevelAdd(int growLevel)
        {
            return growLevel >= MaxGrowLevel ? 1 : 0;
        }

        private static int ClampLevel(int growLevel)
        {
            return Mathf.Clamp(growLevel, 1, MaxGrowLevel);
        }
    }
}
