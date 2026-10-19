using System;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 新手引导进度存档
    /// </summary>
    [Serializable]
    public class GuideData
    {
        //0=等待玩家打开角色界面(只发光提示), 1=选技能页签, 2=选技能, 3=点升级, 4=完成提示, 99=已完成
        public int homeSkillStep;

        public bool homeSkillDone;

        public void CheckNull()
        {
            homeSkillStep = Mathf.Clamp(homeSkillStep, 0, 99);
        }
    }
}
