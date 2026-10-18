using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    public abstract class PrefabsTabPanel : TabStandardPanel
    {
        public UIPrefabs prefabs;
        protected T AddPanel<T>(string panelName, string findPanel = "") where T : SubPanel, new()
        {
            T subPanel = new T();

            //SubTitle 名子
            //创建 Tab & subPanel
            if (string.IsNullOrEmpty(findPanel))
            {
                GetSubPanelGo(subPanel);
            }
            else
            {
                FindSubPanel(subPanel, findPanel);
            }


            subPanel.Prefabs = prefabs;
            subPanel.subPanelName = panelName;

            int panelIndex = list.Count;

            var tabBtnTf = TABS.GetChild(panelIndex);
            if (tabBtnTf == null && panelIndex != 0)
            {
                if (panelIndex != 0)
                {

                    UITool.SetCellListCount(TABS, panelIndex + 1);
                    tabBtnTf = TABS.GetChild(panelIndex);
                }
                else
                {
                    Debug.LogError("no tab btn");
                }

            }

            var tabBtn = tabBtnTf.GetComponentInChildren<Button>();
            subPanel.TabBtn = tabBtn;
            tabBtn.onClick.AddListener(() => { SwitchPanel(panelName); });
            tabBtnTf.GetComponentInChildren<Button>().Select();
            //tabBtnTf.GetComponentInChildren<TextMeshProUGUI>().BindLocalizer(panelName);

            subPanel.Init();
            list.Add(subPanel);
            return subPanel;
        }


        protected void GetSubPanelGo(SubPanel subPanel)
        {
            GameObject panelGo = Instantiate(prefabs.subPanel, PANELS);
            subPanel.gameObject = panelGo;
        }

        protected void FindSubPanel(SubPanel subPanel, string findPanel)
        {
            GameObject panelGo = PANELS.Find(findPanel).gameObject;
            subPanel.gameObject = panelGo;
        }
    }
}