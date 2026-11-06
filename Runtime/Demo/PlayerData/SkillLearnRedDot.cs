using System.Collections.Generic;
using TEngine;
using UnityEngine;
using cfg;

namespace XiaoCao
{
    /// <summary>
    /// 技能学习红点规则: 判断技能是否达到学习/升级条件(角色等级达标 + 材料充足), 并记录查看状态。
    /// 技能格 / 技能页签 / 角色面板入口按钮三级红点共用同一套判断。
    /// </summary>
    public static class SkillLearnRedDot
    {
        //全部可学技能, 只在首次使用时读一次配置
        private static List<string> allSkillIds;

        private static List<string> AllSkillIds
        {
            get
            {
                if (allSkillIds == null)
                {
                    PlayerSkillSo setting = Resources.Load<PlayerSkillSo>("PlayerSkillSo");
                    allSkillIds = setting.GetSkillList(0);
                }

                return allSkillIds;
            }
        }

        /// <summary>
        /// 是否存在需要提示的技能(可学习/升级, 且该等级还没被查看过)
        /// </summary>
        public static bool HasAnyNew()
        {
            List<string> skillIds = AllSkillIds;
            for (int i = 0; i < skillIds.Count; i++)
            {
                if (IsNew(skillIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 该技能是否有需要提示的红点: 可学习/升级, 且当前等级还没被查看过
        /// </summary>
        public static bool IsNew(string skillId)
        {
            return CanLearn(skillId) && !IsRead(skillId);
        }

        /// <summary>
        /// 是否满足学习/升级条件: 未满级 + 角色等级达标 + 材料充足(与技能详情面板的升级按钮校验一致)
        /// </summary>
        public static bool CanLearn(string skillId)
        {
            //配置表与存档都依赖资源初始化完成
            if (!ResMgr.IsLoadBaseFinish || string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            PlayerSaveData saveData = PlayerSaveData.LocalSavaData;
            SkillUpgradeSetting setting = LubanTables.GetSkillUpgradeSetting(skillId);
            int curLv = saveData.GetSkillLevel(skillId);
            if (curLv >= setting.MaxLevel)
            {
                return false;
            }

            if (saveData.lv < setting.NeedLv)
            {
                return false;
            }

            List<Item> costItems = LubanTables.GetSkillUpgradeItems(skillId, curLv + 1);
            return saveData.inventory.CheckEnoughItemList(costItems, false);
        }

        /// <summary>
        /// 当前技能等级是否已经被查看过: 等级变化(升级)后会重新提示
        /// </summary>
        private static bool IsRead(string skillId)
        {
            PlayerSaveData saveData = PlayerSaveData.LocalSavaData;
            return saveData.skillRedDotReadLv.TryGetValue(skillId, out int readLv) && readLv == saveData.GetSkillLevel(skillId);
        }

        /// <summary>
        /// 标记该技能当前等级的红点已查看, 并广播刷新页签与入口按钮
        /// </summary>
        public static void MarkRead(string skillId)
        {
            if (!CanLearn(skillId) || IsRead(skillId))
            {
                return;
            }

            PlayerSaveData saveData = PlayerSaveData.LocalSavaData;
            saveData.skillRedDotReadLv[skillId] = saveData.GetSkillLevel(skillId);
            //SavaData 是静态方法, 只能用类型名调用
            PlayerSaveData.SavaData();
            NotifyChanged();
        }

        /// <summary>
        /// 广播红点变化, 让页签与面板入口按钮重新计算(技能升级后也需要调用)
        /// </summary>
        public static void NotifyChanged()
        {
            GameEvent.Send(EGameEvent.SkillRedDotChange.ToInt());
        }
    }
}
