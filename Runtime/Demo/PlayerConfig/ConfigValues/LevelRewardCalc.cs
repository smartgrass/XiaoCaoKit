using System;

namespace XiaoCao
{
    public static class LevelRewardCalc
    {
        public static int GetExp(string levelName)
        {
            LevelInfo info = LevelInfo.ParseString(levelName);
            int index = info.index;
            int baseExp = GetChapterBaseExp(info.chapter);
            return GetLevelRewardExp(index, baseExp);
        }

        
        public static int GetCoin(string levelName)
        {
            return GetExp(levelName) * 5;
        }

        

        //每 5 关一组：前 4 关为基础经验，第 5 关（组尾）翻倍
        //每组提升经验 addExp=10
        public static int GetLevelRewardExp(int levelIndex, int chapterBaseExp)
        {
            const int addExp = 20;
            // 组号，向上取整
            int group = (levelIndex + 4) / 5;
            int baseGroupExp = addExp * group + chapterBaseExp;
            bool isGroupLast = (levelIndex % 5) == 0;
            int exp = isGroupLast ? baseGroupExp * 2 : baseGroupExp;
            return exp;
        }
        
        /// <summary>
        /// 计算关卡首通奖励
        /// </summary>
        /// <param name="levelIndex">关卡编号，从1开始</param>
        /// <returns>首通经验值</returns>
        public static int CalcFirstPassExp(int levelIndex)
        {
            int group = (levelIndex + 4) / 5;
            float baseFirst;

            // ========== 1~4关特殊逻辑【预留，填入你的数值】==========
            if (levelIndex >= 1 && levelIndex <= 4)
            {
                // 示例，替换成你实际需求：
                switch (levelIndex)
                {
                    case 1: baseFirst = 50; break;
                    case 2: baseFirst = 60; break;
                    case 3: baseFirst = 70; break;
                    case 4: baseFirst = 80; break;
                    default: baseFirst = 100; break;
                }
            }
            // ========== 5 ~ 30关 ==========
            else if (levelIndex >=5 && levelIndex <= 30)
            {
                baseFirst = 100;
            }
            // ========== >=31关，绑定group增长 ==========
            else
            {
                baseFirst = 100f * (1 + (group - 6) * 0.35f);
            }

            // 5倍数大关，首通翻倍
            if (levelIndex % 5 == 0)
            {
                baseFirst *= 2;
            }

            return (int)Math.Round(baseFirst);
        }

        /// <summary>
        /// 按关卡名计算首通经验值(关卡编号取章节内序号,和普通通关经验取值方式一致)
        /// </summary>
        public static int CalcFirstPassExp(string levelName)
        {
            LevelInfo info = LevelInfo.ParseString(levelName);
            return CalcFirstPassExp(info.index);
        }

        /// <summary>
        /// 通关矿石配置: 首通奖励与精英关的每次通关奖励共用这份数量配置
        /// </summary>
        public struct ClearOreSetting
        {
            public int MaxLevel;   //累计覆盖到的关卡数
            public string ItemId;  //矿石id
            public int Count;      //每个关卡每次发放的基础数量
            public int ExCount;    //精英关(5的倍数关)的额外步进数量
        }

        //精英关间隔: 每 5 关一个精英关
        public const int EliteLevelStep = 5;

        /// <summary>
        /// 通关矿石配置表: MaxLevel 是累计上限, 按 MaxLevel 从小到大填写。
        /// 例: (20, Crystal_B_0, 2, 2) 表示 1~20 关每关发 2 个 Crystal_B_0, 精英关(5的倍数关)按
        /// (关卡/5 - 1) * 0.5 * ExCount 递增: 第5关 2 个, 第10关 3 个, 第15关 4 个;
        /// 再配一条 (30, Crystal_B_1, 2, 2) 就表示 21~30 关的配置, 以此类推; 超出最后一条 MaxLevel 的关卡不给矿石。
        /// </summary>
        public static readonly ClearOreSetting[] ClearOreSettings =
        {
            new ClearOreSetting { MaxLevel = 20, ItemId = "Crystal_B_0", Count = 2, ExCount = 2 },
        };

        /// <summary>
        /// 按关卡编号取首通矿石奖励: 所有配置到的关卡都给(含精英关), 该关没配置时返回 null
        /// </summary>
        /// <param name="levelIndex">关卡编号, 从1开始(章节内序号)</param>
        public static Item GetFirstPassOre(int levelIndex)
        {
            return CreateOre(levelIndex, false);
        }

        /// <summary>
        /// 按关卡编号取每次通关(重复刷)的矿石奖励: 只有精英关(5的倍数关)给, 其余关卡返回 null
        /// </summary>
        /// <param name="levelIndex">关卡编号, 从1开始(章节内序号)</param>
        public static Item GetClearOre(int levelIndex)
        {
            return CreateOre(levelIndex, true);
        }

        /// <summary>
        /// 按配置拼出矿石物品
        /// </summary>
        /// <param name="levelIndex">关卡编号, 从1开始(章节内序号)</param>
        /// <param name="eliteOnly">是否只在精英关(5的倍数关)发放</param>
        private static Item CreateOre(int levelIndex, bool eliteOnly)
        {
            if (eliteOnly && levelIndex % EliteLevelStep != 0)
            {
                return null;
            }

            foreach (ClearOreSetting setting in ClearOreSettings)
            {
                //命中第一条覆盖到该关卡的配置, 前一条 MaxLevel+1 到本条 MaxLevel 即为它的关卡段
                if (levelIndex > setting.MaxLevel)
                {
                    continue;
                }

                //矿石id形如 Crystal_B_0, 品阶由id末位推出(CrystalItem.Create 内部解析)
                CrystalItem crystal = CrystalItem.Create(setting.ItemId);
                crystal.count = GetClearOreCount(setting, levelIndex);
                return crystal.ToItem();
            }

            return null;
        }

        /// <summary>
        /// 取某关在该段配置下的矿石数量: 普通关为基础数量, 精英关(5的倍数关)再加精英加成。
        /// 精英加成为 (关卡/5 - 1) * 0.5 * ExCount, 即第5关不加成, 第10关加 0.5*ExCount, 第15关加 1*ExCount, 结果四舍五入。
        /// </summary>
        public static int GetClearOreCount(ClearOreSetting setting, int levelIndex)
        {
            float count = setting.Count;
            if (levelIndex % EliteLevelStep == 0)
            {
                int eliteIndex = levelIndex / EliteLevelStep;
                count += (eliteIndex - 1) * 0.5f * setting.ExCount;
            }

            return (int)Math.Round(count, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 按关卡名取首通矿石奖励(关卡编号取章节内序号)
        /// </summary>
        public static Item GetFirstPassOre(string levelName)
        {
            LevelInfo info = LevelInfo.ParseString(levelName);
            return GetFirstPassOre(info.index);
        }

        /// <summary>
        /// 按关卡名取每次通关(重复刷)的矿石奖励(关卡编号取章节内序号)
        /// </summary>
        public static Item GetClearOre(string levelName)
        {
            LevelInfo info = LevelInfo.ParseString(levelName);
            return GetClearOre(info.index);
        }


        

        public static int CalcTotalExp(int startIndex, int endIndex, int chapterIndex)
        {
            int totalExp = 0;
            int baseExp = GetChapterBaseExp(chapterIndex);
            for (int levelIndex = startIndex; levelIndex <= endIndex; levelIndex++)
            {
                totalExp += GetLevelRewardExp(levelIndex, baseExp);
            }

            return totalExp;
        }


        private static int GetChapterBaseExp(int chapter)
        {
            //第一章默认0
            if (chapter == 0)
            {
                return 0;
            }
            else if (chapter == 1)
            {
                //暂定
                return 100;
            }

            return chapter * 100;
        }
    }
}
