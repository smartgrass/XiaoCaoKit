using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 友方支援攻击状态。
    /// 复用 AtkState 的前摇、后摇和受击流程，但攻击技能会从玩家已学会技能中动态抽取。
    /// </summary>
    [CreateAssetMenu(fileName = "FriendAtkState", menuName = "SO/AI/FriendAtkState", order = 1)]
    public class FriendAtkState : AtkState
    {
        [XCLabel("技能抽取方式")]
        public FSMPoolType drawType = FSMPoolType.Random;

        [XCLabel("单轮最多抽取技能数")]
        [Tooltip("小于等于 0 时表示本轮使用全部已学会技能。")]
        public int maxSkillTime = 0;

        private readonly List<string> _runtimeSkillPool = new List<string>();
        private readonly List<string> _allLearnedSkillIds = new List<string>();

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
        /// </summary>
        protected override string GetAtkMsg()
        {
            if (_runtimeSkillPool.Count == 0)
            {
                RebuildRuntimeSkillPool();
            }

            if (_runtimeSkillPool.Count == 0)
            {
                return base.GetAtkMsg();
            }

            int skillIndex = drawType == FSMPoolType.Random
                ? UnityEngine.Random.Range(0, _runtimeSkillPool.Count)
                : 0;

            string skillId = _runtimeSkillPool[skillIndex];
            _runtimeSkillPool.RemoveAt(skillIndex);
            return skillId;
        }

        /// <summary>
        /// 按当前玩家已学会技能刷新本轮可用技能池。
        /// 如果配置了 maxSkillTime，则本轮最多只保留对应数量的技能。
        /// </summary>
        private void RebuildRuntimeSkillPool()
        {
            _runtimeSkillPool.Clear();
            RefreshLearnedSkillIds();
            if (_allLearnedSkillIds.Count == 0)
            {
                return;
            }

            int takeCount = maxSkillTime > 0
                ? Mathf.Min(maxSkillTime, _allLearnedSkillIds.Count)
                : _allLearnedSkillIds.Count;

            if (drawType == FSMPoolType.Random)
            {
                List<string> tempList = new List<string>(_allLearnedSkillIds);
                for (int i = 0; i < takeCount && tempList.Count > 0; i++)
                {
                    int randomIndex = UnityEngine.Random.Range(0, tempList.Count);
                    _runtimeSkillPool.Add(tempList[randomIndex]);
                    tempList.RemoveAt(randomIndex);
                }
            }
            else
            {
                for (int i = 0; i < takeCount; i++)
                {
                    _runtimeSkillPool.Add(_allLearnedSkillIds[i]);
                }
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
