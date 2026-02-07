using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;

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


            subPanel.standardPanel = this;
            subPanel.Prefabs = prefabs;
            subPanel.subPanelName = panelName;

            GameObject tabBtnGo = Instantiate(prefabs.tabBtn, TABS);
            var tabBtn = tabBtnGo.GetComponentInChildren<Button>();
            subPanel.TabBtn = tabBtn;
            tabBtn.onClick.AddListener(() => { SwitchPanel(panelName); });
            tabBtnGo.GetComponentInChildren<Button>().Select();
            tabBtnGo.GetComponentInChildren<TextMeshProUGUI>().BindLocalizer(panelName);

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