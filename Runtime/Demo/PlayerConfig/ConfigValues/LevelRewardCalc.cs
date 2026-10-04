namespace XiaoCao
{
    public static class LevelRewardCalc
    {
        public static int GetExp(string levelName)
        {
            LevelInfo info = LevelInfo.ParseString(levelName);
            int index = info.index;
            int baseExp = GetChapterBaseExp(info.chapter);
            return GetLevelExp(index, baseExp);
        }

        
        public static int GetCoin(string levelName)
        {
            return GetExp(levelName) * 5;
        }

        

        //每 5 关一组：前 4 关为基础经验，第 5 关（组尾）翻倍
        //每组提升经验 addExp=10
        public static int GetLevelExp(int levelIndex, int chapterBaseExp)
        {
            const int addExp = 10;
            // 组号，向上取整
            int group = (levelIndex + 4) / 5;
            int baseGroupExp = addExp * group + chapterBaseExp;
            bool isGroupLast = (levelIndex % 5) == 0;
            int exp = isGroupLast ? baseGroupExp * 2 : baseGroupExp;
            return exp;
        }
        

        public static int CalcTotalExp(int startIndex, int endIndex, int chapterIndex)
        {
            int totalExp = 0;
            int baseExp = GetChapterBaseExp(chapterIndex);
            for (int levelIndex = startIndex; levelIndex <= endIndex; levelIndex++)
            {
                totalExp += GetLevelExp(levelIndex, baseExp);
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