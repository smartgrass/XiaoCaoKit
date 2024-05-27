using cfg;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class LevelSettingHelper
    {

        internal static string GetText(int v)
        {
            return LubanTables.GetLevelSetting(v).Title;
            //return $"level{v}";
        }

        /// <summary>
        /// 章节就是关卡号的百位数
        /// </summary>
        /// <returns></returns>
        public static int GetChapterByLevel(int level)
        {
            return level % 100 +1;
        }

    }
}
