namespace XiaoCao
{
    [XCHelper]
    public static class LevelDataHelper
    {
        public static LevelPassState GetLevelState(string levelKey)
        {
            LevelInfo levelInfo = LevelInfo.ParseString(levelKey);
            LevelPassState passState =
                PlayerSaveData.LocalSavaData.levelPassData.GetPassState(levelInfo.chapter, levelInfo.index);
            return passState;
        }
        
        
    }
        
    /// <see cref="MapNames.GetLevelInfoByName"/>
    public struct LevelInfo
    {
        public int chapter;
        public int index;

        public string GetLevelName()
        {
            return LocalizeKey.GetLevelName(chapter, index);
        }

        public static LevelInfo ParseString(string levelKey)
        {
            var array = levelKey.Split('_');

            return new LevelInfo()
            {
                chapter = int.Parse(array[1]),
                index = int.Parse(array[2])
            };
        }
        
        public static string GetLevelKey(int chapter, int index)
        {
            return $"level_{chapter}_{index}";
        }

    }
}