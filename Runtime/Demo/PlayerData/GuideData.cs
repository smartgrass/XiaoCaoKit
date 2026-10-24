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
        //引导1(技能2 撞击): 0=等待玩家打开角色界面(只发光提示), 1=选技能页签, 2=选技能,
        //3=点升级, 4=点返回, 5=完成提示(弹 Toast 后立即结束), 99=已完成
        public int homeSkillStep;

        public bool homeSkillDone;

        //引导2(技能1 凌空连击): 步骤含义与引导1一致
        public int homeSkill2Step;

        public bool homeSkill2Done;

        public void CheckNull()
        {
            homeSkillStep = Mathf.Clamp(homeSkillStep, 0, 99);
            homeSkill2Step = Mathf.Clamp(homeSkill2Step, 0, 99);
        }
    }
}
