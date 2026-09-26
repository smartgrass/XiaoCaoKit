namespace XiaoCao
{
    [XCHelper]
    public static class UnlockDataHelper
    {
        enum EUnlockType
        {
            Level,
            Mark
        }

        public static bool CheckUnlock(string key)
        {
            string[] infos = key.Split(",");

            if (infos[0] == EUnlockType.Level.ToString())
            {
                LevelInfo levelInfo = LevelInfo.ParseString(infos[1]);
                LevelPassState passState = PlayerSaveData.LocalSavaData.
                    levelPassData.GetPassState(levelInfo.chapter, levelInfo.index);
                return passState == LevelPassState.Pass;
            }

            return false;
        }


        //获取 解锁方式描述
        public static string GetUnlockLocalize(string key)
        {
            string[] infos = key.Split(",");

            if (infos[0] == EUnlockType.Level.ToString())
            {
                return infos[1];
            }

            return "Unknown";
        }
    }
}