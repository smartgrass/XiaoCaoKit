using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 友方支援攻击状态(只负责技能)。
    /// 复用 AtkState 的前摇、后摇和受击流程，但攻击技能会从玩家已学会技能中动态抽取。
    /// 可用技能数由 <see cref="SupportRoleSetting"/> 的技巧养成等级决定, 默认只解锁 1 个。
    /// 普攻由上层 <see cref="FriendAtkFSM"/> 负责调度。
    /// </summary>
    [CreateAssetMenu(fileName = "FriendAtkState", menuName = "SO/AI/FriendAtkState", order = 1)]
    public class FriendAtkState : AtkState
    {
        [XCLabel("技能抽取方式")]
        public FSMPoolType drawType = FSMPoolType.Random;

        [XCLabel("单轮最多抽取技能数")]
        [Tooltip("小于等于 0 时表示本轮使用全部已学会技能。")]
        public int maxSkillTime = 0;

        [XCLabel("技能CD(秒)")]
        [Tooltip("使用某技能后, 该id在此时间内不可再次使用; 小于等于 0 表示无CD。")]
        public float skillCd = 0;

        [XCLabel("技能连击上限次数")]
        [Tooltip("连续使用 n 次技能后, 本状态不可再释放技能(上层会切换到普攻); 小于等于 0 表示无限制。")]
        public int skillComboTime = 0;

        /// <summary>各技能 id 的 CD 结束时间</summary>
        private readonly Dictionary<string, float> _skillReadyTime = new Dictionary<string, float>();

        /// <summary>当前连续使用技能的次数</summary>
        private int _skillComboCount;

        private readonly List<string> _runtimeSkillPool = new List<string>();
        private readonly List<string> _allLearnedSkillIds = new List<string>();

        /// <summary>
        /// 当前是否可释放技能: 未达连击上限且存在 CD 已好的技能。
        /// 供上层 <see cref="FriendAtkFSM"/> 决定用技能还是普攻。
        /// </summary>
        public bool CanUseSkill()
        {
            if (skillComboTime > 0 && _skillComboCount >= skillComboTime)
            {
                return false;
            }
            if (_runtimeSkillPool.Count == 0)
            {
                RebuildRuntimeSkillPool();
            }
            return _runtimeSkillPool.Exists(IsSkillReady);
        }

        /// <summary>重置技能连击计数(打完一轮普攻后由上层调用)</summary>
        public void ResetCombo()
        {
            _skillComboCount = 0;
        }

        /// <summary>
        /// 友方攻击直接使用技能 id，不走敌人的 AiCmdSetting 索引表。
        /// </summary>
        protected override ActMsgType GetActMsgType()
        {
            return ActMsgType.OtherSkill;
        }

        /// <summary>
        /// 获取本次要释放的技能 id。
        /// 当技能池为空时会重新从玩家已学会技能中构建。
        /// 技能使用后进入 skillCd; CD 中或连击达上限时返回空字符串(本次不攻击, 普攻由上层调度)。
        /// </summary>
        public override string GetAtkMsg()
        {
            if (_runtimeSkillPool.Count == 0)
            {
                RebuildRuntimeSkillPool();
            }

            if (_runtimeSkillPool.Count == 0)
            {
                return "";
            }

            //CD 中的技能不可用
            List<string> readyIds = _runtimeSkillPool.FindAll(IsSkillReady);
            if (readyIds.Count == 0)
            {
                return "";
            }

            int skillIndex = drawType == FSMPoolType.Random
                ? UnityEngine.Random.Range(0, readyIds.Count)
                : 0;

            string skillId = readyIds[skillIndex];
            _runtimeSkillPool.Remove(skillId);

            //记录技能 CD, 更新连击计数
            if (skillCd > 0)
            {
                _skillReadyTime[skillId] = Time.time + skillCd;
            }
            _skillComboCount++;
            return skillId;
        }

        /// <summary>技能是否已结束 CD</summary>
        private bool IsSkillReady(string skillId)
        {
            return !_skillReadyTime.TryGetValue(skillId, out float readyTime) || Time.time >= readyTime;
        }

        /// <summary>
        /// 按当前玩家已学会技能刷新本轮可用技能池。
        /// 支援角色固定解锁玩家已学会技能中 id 升序的前 N 个(默认 1 个, 技巧养成提升 N, 上限 4 个),
        /// 抽取方式只决定在已解锁技能内的选择顺序, 不会绕过解锁数限制。
        /// </summary>
        private void RebuildRuntimeSkillPool()
        {
            _runtimeSkillPool.Clear();
            RefreshLearnedSkillIds();
            if (_allLearnedSkillIds.Count == 0)
            {
                return;
            }

            //已解锁技能: id 升序的前 N 个, 养成升级只会往后追加新技能
            int unlockCount = Mathf.Min(SupportRoleSetting.GetUnlockSkillCount(), _allLearnedSkillIds.Count);

            //单轮抽取上限
            int takeCount = maxSkillTime > 0 ? Mathf.Min(unlockCount, maxSkillTime) : unlockCount;

            List<string> unlockedSkillIds = _allLearnedSkillIds.GetRange(0, unlockCount);

            if (drawType == FSMPoolType.Random)
            {
                //随机打乱已解锁技能后取前 takeCount 个(N=1 时恒为同一个技能)
                for (int i = unlockedSkillIds.Count - 1; i > 0; i--)
                {
                    int randomIndex = UnityEngine.Random.Range(0, i + 1);
                    (unlockedSkillIds[i], unlockedSkillIds[randomIndex]) = (unlockedSkillIds[randomIndex], unlockedSkillIds[i]);
                }
            }

            for (int i = 0; i < takeCount; i++)
            {
                _runtimeSkillPool.Add(unlockedSkillIds[i]);
            }
        }

        /// <summary>
        /// 收集当前玩家已经学会的全部技能，并为顺序抽取提供稳定顺序。
        /// </summary>
        private void RefreshLearnedSkillIds()
        {
            _allLearnedSkillIds.Clear();
            PlayerSaveData playerSaveData = PlayerSaveData.LocalSavaData;
            if (playerSaveData == null || playerSaveData.skillUnlockDic == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> item in playerSaveData.skillUnlockDic)
            {
                if (!string.IsNullOrEmpty(item.Key) && item.Value > 0)
                {
                    _allLearnedSkillIds.Add(item.Key);
                }
            }

            _allLearnedSkillIds.Sort(CompareSkillId);
        }

        /// <summary>
        /// 技能 id 优先按数字顺序排序，无法转数字时再按字符串排序。
        /// </summary>
        private static int CompareSkillId(string left, string right)
        {
            bool leftIsNumber = int.TryParse(left, out int leftNumber);
            bool rightIsNumber = int.TryParse(right, out int rightNumber);
            if (leftIsNumber && rightIsNumber)
            {
                return leftNumber.CompareTo(rightNumber);
            }

            return string.Compare(left, right, StringComparison.Ordinal);
        }
    }
}
