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
    /// 玩家可见的只有换算后的羁绊等级(见 <see cref="GetBondLevel()"/>), 子等级仅作为计算过程;
    /// 羁绊经验由通关关卡获得(每通一次 +1), 满经验自动升羁绊等级并同步三项子等级
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

        /// <summary>羁绊等级上限: 三项养成各到 Lv4(即存档的满级)时为羁绊 Lv9</summary>
        public const int MaxBondLevel = 9;

        /// <summary>羁绊每级递增的经验基数: 升到 Lv2 需 10 点, 升到 Lv3 需 20 点, 之后每级再 +10</summary>
        public const int BondExpStep = 10;

        /// <summary>关卡模式通关一次获得的羁绊经验</summary>
        public const int BondExpPerLevelClear = 1;

        /// <summary>
        /// 指定羁绊等级升到下一级所需经验; 已满级返回 0(不再累计经验)。
        /// </summary>
        public static int GetNextLevelExp(int bondLevel)
        {
            if (bondLevel >= MaxBondLevel)
            {
                return 0;
            }

            return BondExpStep * Mathf.Clamp(bondLevel, 1, MaxBondLevel - 1);
        }

        /// <summary>
        /// 由羁绊等级反算三项养成等级(存档用的 1~5), 与 <see cref="GetBondLevel(int, int, int)"/> 互逆:
        /// 奇数级三项齐平, 偶数级技巧高一层。
        /// </summary>
        public static void GetGrowLevels(int bondLevel, out int atkGrowLevel, out int hpGrowLevel, out int skillGrowLevel)
        {
            int index = Mathf.Clamp(bondLevel, 1, MaxBondLevel) - 1;
            int baseLevel = index / 2;

            atkGrowLevel = baseLevel + 1;
            hpGrowLevel = baseLevel + 1;
            skillGrowLevel = baseLevel + index % 2 + 1;
        }

        /// <summary>
        /// 当前羁绊等级: 玩家只能看到这一层数值, 三项子等级只作为换算过程, 不对外展示
        /// </summary>
        public static int GetBondLevel()
        {
            return GetBondLevel(GetAtkGrowLevel(), GetHpGrowLevel(), GetSkillGrowLevel());
        }

        /// <summary>
        /// 羁绊等级换算: 取三项子等级的公共层数, 技巧高出一层时再 +1。
        /// 即 Lv1=(0,0,0) / Lv2=(0,0,1) / Lv3=(1,1,1) ... Lv9=(4,4,4)
        /// </summary>
        public static int GetBondLevel(int atkGrowLevel, int hpGrowLevel, int skillGrowLevel)
        {
            // 存档里的养成等级从 1 起算, 先还原成设计文档的 Lv0~Lv4
            int atk = ClampLevel(atkGrowLevel) - 1;
            int hp = ClampLevel(hpGrowLevel) - 1;
            int skill = ClampLevel(skillGrowLevel) - 1;

            int baseLevel = Mathf.Min(Mathf.Min(atk, hp), skill);
            int bondLevel = 1 + 2 * baseLevel + (skill > baseLevel ? 1 : 0);
            return Mathf.Clamp(bondLevel, 1, MaxBondLevel);
        }

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
