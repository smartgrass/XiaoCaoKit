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
            subPanel.gameObject.name = panelName;

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
            BindSubPanel(subPanel, panelGo);
            
        }

        protected void FindSubPanel(SubPanel subPanel, string findPanel)
        {
            GameObject panelGo = PANELS.Find(findPanel).gameObject;
            BindSubPanel(subPanel, panelGo);
        }

        /// <summary>
        /// 记录子面板的根节点与条目挂载层: 模板外层套了 ScrollView 时条目挂到 Viewport 内的 Content,
        /// 并把 Content 改成顶部对齐 + 高度自适应, 否则内容超出可视区也不会滚动
        /// </summary>
        private static void BindSubPanel(SubPanel subPanel, GameObject panelGo)
        {
            subPanel.gameObject = panelGo;

            ScrollRect scrollRect = panelGo.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                subPanel.content = (RectTransform)panelGo.transform;
                return;
            }

            subPanel.scroll = scrollRect;

            RectTransform contentRt = scrollRect.content;
            subPanel.content = contentRt;

            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            //只把纵向归零(顶部对齐), 横向保留模板里的偏移, 免得内容整体左右跑
            contentRt.anchoredPosition = new Vector2(contentRt.anchoredPosition.x, 0f);

            if (contentRt.GetComponent<ContentSizeFitter>() == null)
            {
                ContentSizeFitter fitter = contentRt.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
    }
}