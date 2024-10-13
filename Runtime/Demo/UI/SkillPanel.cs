using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public class SkillPanel : SubPanel
    {
        public SkillPanelView view;

        public SkillTreeSo setting;

        private List<SkillCell> cells = new List<SkillCell>();


        public override void Init()
        {
            view = gameObject.GetComponent<SkillPanelView>();

            Debug.Log($"--- SkillPanel init");
            setting = Resources.Load<SkillTreeSo>("SkillTreeSo");


            foreach (var data in setting.datas)
            {
                var cell = GameObject.Instantiate(view.cellPrefab, view.Content);
                SkillCell skillCell = cell.GetComponent<SkillCell>();
                cells.Add(skillCell);
            }

            view.Prefab.gameObject.SetActive(false);
            //点亮和熄灭
            //玩家解锁数据保存
            UpdateState();
        }

        public void UpdateState()
        {
            var dic = PlayerSaveData.Current.skillUnlockDic;
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                int level = 0;
                dic.TryGetValue(cell.skillIndex, out level);
                cell.SetUnlock(level > 0);

                var data = setting.datas[i];
                Vector3 pos = new Vector3(data.pos.x * setting.posScale.x, data.pos.y * setting.posScale.y);
                (cell.transform as RectTransform).localPosition = pos;
            }
        }
    }


}