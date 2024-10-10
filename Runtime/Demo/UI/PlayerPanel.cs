using NaughtyAttributes;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace XiaoCao
{
    /// <summary>
    /// 背包, 技能界面
    /// </summary>
    public class PlayerPanel : TabPanel
    {
        public override UIPanelType panelType => UIPanelType.PlayerPanel;
        public override void Init()
        {
            if (IsInited)
            {
                return;
            }
            base.Init();


            var skillPanel = AddPanel<SkillPanel>("Skill", "SkillPanel");
           
            IsInited = true;
        }


        [Button]
        void UpdateSkill()
        {
            (GetPanel("Skill") as SkillPanel).UpdateState();
        }



    }
}