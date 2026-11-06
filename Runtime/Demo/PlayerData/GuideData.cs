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

        //引导3(主城任务点): 0=导航+提示玩家去任务点查看任务奖励, 99=已完成(点开并关闭任务界面后完成)
        public int homeTaskStep;

        public bool homeTaskDone;

        //引导3附加项(圣器一键装备, 可跳过): 0=等背包出现未装备圣器, 1=等打开圣器界面, 2=等点一键装备, 99=已完成
        public int homeRelicStep;

        public bool homeRelicDone;

        public void CheckNull()
        {
            homeSkillStep = Mathf.Clamp(homeSkillStep, 0, 99);
            homeSkill2Step = Mathf.Clamp(homeSkill2Step, 0, 99);
            homeTaskStep = Mathf.Clamp(homeTaskStep, 0, 99);
            homeRelicStep = Mathf.Clamp(homeRelicStep, 0, 99);
        }
    }
}
