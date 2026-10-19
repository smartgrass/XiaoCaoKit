using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 支援角色数值设定, 规则见 Assets/_Md/系统设计/支援角色设定.md
    /// 初始状态: 继承玩家 无等级默认数据(无加成状态) 的 30% 攻击 / 50% 血量, 解锁 1 技能
    /// 养成提升(每项 Lv1~Lv5):
    /// - 攻击: 30% -> 80% (线性)
    /// - 生存: 50% -> 100% (线性)
    /// - 技巧: 每级解锁 1 个技能(上限 4 个), 满级后支援使用的技能等级 +1
    /// 升级方式和浏览界面目前暂未实装, 三项养成等级恒为初始值 1,
    /// 后续接入存档时把 <see cref="GetAtkGrowLevel"/> 等方法改为读存档即可
    /// </summary>
    public static class SupportRoleSetting
    {
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

        /// <summary>攻击养成等级, 当前未实装养成, 恒为初始值</summary>
        public static int GetAtkGrowLevel()
        {
            //TODO 接入存档: return PlayerSaveData.LocalSavaData.supportRoleAtkLv;
            return DefaultGrowLevel;
        }

        /// <summary>生存养成等级, 当前未实装养成, 恒为初始值</summary>
        public static int GetHpGrowLevel()
        {
            //TODO 接入存档: return PlayerSaveData.LocalSavaData.supportRoleHpLv;
            return DefaultGrowLevel;
        }

        /// <summary>技巧养成等级, 当前未实装养成, 恒为初始值</summary>
        public static int GetSkillGrowLevel()
        {
            //TODO 接入存档: return PlayerSaveData.LocalSavaData.supportRoleSkillLv;
            return DefaultGrowLevel;
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
        /// 支援角色使用玩家技能时的等级加成(技巧满级后 +1)
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
