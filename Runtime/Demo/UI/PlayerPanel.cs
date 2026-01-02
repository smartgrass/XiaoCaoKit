using NaughtyAttributes;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 背包, 技能界面
    /// </summary>
    public class PlayerPanel : TabStandardPanel
    {
        public override UIPanelType PanelType => UIPanelType.PlayerPanel;
        public override void Init()
        {
            if (IsInited)
            {
                return;
            }
            base.Init();

            var buffPanel = AddPanel<BuffPanel>("Buff", "BuffPanel");
            var playerShowPanel = AddPanel<PlayerShowPanel>("Role", "PlayerShowPanel");

            //var skillPanel = AddPanel<SkillPanel>("Skill", "SkillPanel");

            buffPanel.Show();

            gameObject.SetActive(false);
            Prefabs.gameObject.SetActive(false);
            IsInited = true;
        }


        [Button]
        void UpdateSkill()
        {
            (GetPanel("Skill") as SkillPanel).UpdateUI();
        }

        private void OnEnable()
        {
            if (IsInited)
            {
                curPanel?.RefreshUI();
            }
        }


    }
}