using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{

    public class SkillPanel : SubPanel
    {
        public SkillPanelView view;

        public SkillTreeSo setting;

        private List<SkillItemCell> cells = new List<SkillItemCell>();


        public override void Init()
        {
            view = gameObject.GetComponent<SkillPanelView>();

            Debug.Log($"--- SkillPanel init");
            setting = Resources.Load<SkillTreeSo>("SkillTreeSo");

            Transform cellParent = view.unequippedBuffContainer.transform;

            cells = new List<SkillItemCell>(cellParent.GetComponentsInChildren<SkillItemCell>());

            UpdateUI();
        }

        public void UpdateUI()
        {

            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                //1.显示所有技能图标
                cell.skillId = cell.name;
                //2.解锁状态
                int level = PlayerHelper.GetSkillLevel(cell.skillId);
                cell.IsUnlock = level > 0;

                cell.UpdateUI();

            }
        }
    }


}