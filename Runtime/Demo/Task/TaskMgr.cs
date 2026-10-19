using System.Collections.Generic;
using XiaoCao.UI;

namespace XiaoCao
{
    /// <summary>
    /// 任务系统入口: 读取任务配置, 计算任务进度, 发放任务奖励
    /// 进度分两类:
    /// - 累计型(如累计升级技能): 进度存在 <see cref="TaskData"/>, 由数据层在对应行为发生时调用 <see cref="AddCondProgress"/> 累加
    /// - 状态型(如通关某关/拥有某圣器): 每次读取时根据当前存档状态实时计算
    /// </summary>
    public static class TaskMgr
    {
        private static List<cfg.TaskSetting> AllTasks => cfg.LubanTables.Inst.Tables.TaskSettingReader.DataList;

        /// <summary>
        /// 获取全部分类, 按配置表中的出现顺序排列
        /// </summary>
        public static List<string> GetCategories()
        {
            List<string> categories = new List<string>();
            foreach (var setting in AllTasks)
            {
                if (!categories.Contains(setting.Category))
                {
                    categories.Add(setting.Category);
                }
            }
            return categories;
        }

        /// <summary>
        /// 获取指定分类下的任务, 保持配置表顺序
        /// </summary>
        public static List<cfg.TaskSetting> GetTasksByCategory(string category)
        {
            return AllTasks.FindAll(t => t.Category == category);
        }

        public static cfg.TaskSetting GetTask(string taskId)
        {
            return AllTasks.Find(t => t.Id == taskId);
        }

        /// <summary>
        /// 获取任务当前进度(0~目标值): 状态进度与累计进度取较大值
        /// </summary>
        public static int GetProgress(cfg.TaskSetting setting)
        {
            TaskRecord record = GameAllData.playerSaveData.taskData.GetOrAddRecord(setting.Id);
            int progress = System.Math.Max(record.progress, GetStateProgress(setting));
            return System.Math.Min(progress, setting.TargetNum);
        }

        /// <summary>
        /// 任务是否已完成(进度达标)
        /// </summary>
        public static bool IsCompleted(cfg.TaskSetting setting)
        {
            return GetProgress(setting) >= setting.TargetNum;
        }

        /// <summary>
        /// 奖励是否已领取
        /// </summary>
        public static bool IsClaimed(cfg.TaskSetting setting)
        {
            return GameAllData.playerSaveData.taskData.GetOrAddRecord(setting.Id).claimed;
        }

        /// <summary>
        /// 是否可领取: 已完成且未领取
        /// </summary>
        public static bool CanClaim(cfg.TaskSetting setting)
        {
            return IsCompleted(setting) && !IsClaimed(setting);
        }

        /// <summary>
        /// 任务描述文本: 默认按条件类型取通用模板(TaskDesc_[条件类型])并填充参数,
        /// 配置了 descKey 时以配置的 key 为准(用于个别需要特殊描述的任务)
        /// </summary>
        public static string GetDesc(cfg.TaskSetting setting)
        {
            string key = string.IsNullOrEmpty(setting.DescKey)
                ? GetDefaultDescKey(setting.CondType)
                : setting.DescKey;

            object[] args = GetDescArgs(setting);
            if (args == null || args.Length == 0)
            {
                return key.ToLocalizeStr();
            }
            return LocalizeKey.FormatWithArgs(key, args);
        }

        /// <summary>
        /// 条件类型对应的通用描述模板key, 新增条件类型时只需在本地化里补 TaskDesc_[条件类型]
        /// </summary>
        public static string GetDefaultDescKey(cfg.task.TaskCondType condType)
        {
            return $"TaskDesc_{condType}";
        }

        /// <summary>
        /// 描述模板的填充参数, 与模板里的 {0}/{1} 依次对应
        /// </summary>
        private static object[] GetDescArgs(cfg.TaskSetting setting)
        {
            switch (setting.CondType)
            {
                case cfg.task.TaskCondType.SkillUpgradeCount:
                case cfg.task.TaskCondType.SkillUnlockCount:
                    //目标次数/个数
                    return new object[] { setting.TargetNum };
                case cfg.task.TaskCondType.PassLevel:
                    //参数格式: 章节_序号
                    return new object[] { GetPassLevelName(setting.CondParam) };
                case cfg.task.TaskCondType.OwnRelic:
                    //参数: 圣器id, 显示基础名(不带等级)
                    return new object[] { GetRelicName(setting.CondParam) };
                default:
                    return null;
            }
        }

        /// <summary>
        /// "章节_序号" 转关卡显示名, 解析失败时原样返回
        /// </summary>
        private static string GetPassLevelName(string condParam)
        {
            string[] parts = condParam.Split('_');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int chapter) &&
                int.TryParse(parts[1], out int index))
            {
                return LocalizeKey.GetLevelName(chapter, index);
            }
            return condParam;
        }

        /// <summary>
        /// 圣器id 转基础名(不带Lv), 解析失败时原样返回
        /// </summary>
        private static string GetRelicName(string relicId)
        {
            try
            {
                return RelicItem.Create(relicId).GetBaseName();
            }
            catch (System.Exception e)
            {
                Debuger.LogError($"-- task relic param error {relicId} {e.Message}");
                return relicId;
            }
        }

        /// <summary>
        /// 累加指定条件类型下所有任务的进度, 在对应游戏行为发生时由数据层调用
        /// </summary>
        public static void AddCondProgress(cfg.task.TaskCondType condType, int delta)
        {
            TaskData taskData = GameAllData.playerSaveData.taskData;
            foreach (var setting in AllTasks)
            {
                if (setting.CondType == condType)
                {
                    taskData.AddProgress(setting.Id, delta);
                }
            }
        }

        /// <summary>
        /// 领取任务奖励: 标记已领取并发放奖励, 广播任务变化事件
        /// </summary>
        public static void Claim(cfg.TaskSetting setting)
        {
            if (!CanClaim(setting))
            {
                return;
            }

            GameAllData.playerSaveData.taskData.GetOrAddRecord(setting.Id).claimed = true;
            foreach (Item item in setting.Reward)
            {
                item.RewardItem();
            }
            PlayerSaveData.SavaData();
            UICanvasMgr.Inst.EventSystem.SendEvent(XiaoCao.UI.UIEventNames.TaskChange);
        }

        /// <summary>
        /// 任务跳转: 关闭任务界面并打开跳转类型对应的面板
        /// </summary>
        public static void GotoTask(cfg.TaskSetting setting)
        {
            UIMgr.Inst.HideView(UIPanelType.TaskPanel);
            switch (setting.JumpType)
            {
                case "Level":
                    UIMgr.Inst.ShowView(UIPanelType.LevelSelectPanel);
                    break;
                case "Forge":
                    UIMgr.Inst.ShowView(UIPanelType.ForgePanel);
                    break;
                case "Skill":
                    UIMgr.Inst.ShowView(UIPanelType.PlayerPanel);
                    break;
            }
        }

        #region Debug

        /// <summary>
        /// 调试: 把任务进度直接置为完成(可领取)
        /// </summary>
        public static bool DebugCompleteTask(PlayerSaveData saveData, string taskId)
        {
            cfg.TaskSetting setting = GetTask(taskId);
            if (setting == null)
            {
                return false;
            }

            saveData.taskData.GetOrAddRecord(taskId).progress = setting.TargetNum;
            return true;
        }

        /// <summary>
        /// 调试: 把全部任务置为完成
        /// </summary>
        public static void DebugCompleteAll(PlayerSaveData saveData)
        {
            foreach (var setting in AllTasks)
            {
                DebugCompleteTask(saveData, setting.Id);
            }
        }

        /// <summary>
        /// 调试: 清空全部任务记录
        /// </summary>
        public static void DebugClearAll(PlayerSaveData saveData)
        {
            saveData.taskData.ClearAll();
        }

        #endregion

        /// <summary>
        /// 状态型任务的实时进度, 由当前存档状态推导
        /// </summary>
        private static int GetStateProgress(cfg.TaskSetting setting)
        {
            PlayerSaveData saveData = GameAllData.playerSaveData;
            switch (setting.CondType)
            {
                case cfg.task.TaskCondType.SkillUpgradeCount:
                    //纯累计型, 进度只看存档记录
                    return 0;
                case cfg.task.TaskCondType.SkillUnlockCount:
                    //当前已解锁的技能数量
                    return saveData.skillUnlockDic.Count;
                case cfg.task.TaskCondType.PassLevel:
                    //参数格式: 章节_序号
                    string[] parts = setting.CondParam.Split('_');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int chapter) &&
                        int.TryParse(parts[1], out int index) &&
                        saveData.levelPassData.GetPassState(chapter, index) == LevelPassState.Pass)
                    {
                        return 1;
                    }
                    return 0;
                case cfg.task.TaskCondType.OwnRelic:
                    //背包与已装备的圣器都算拥有
                    int count = saveData.inventory.GetItemCount(setting.CondParam);
                    foreach (Item item in saveData.inventory.equippedHolyRelics)
                    {
                        if (item != null && item.type == ItemType.HolyRelic && item.ItemKey == setting.CondParam)
                        {
                            count += 1;
                        }
                    }
                    return count;
                default:
                    return 0;
            }
        }
    }
}
