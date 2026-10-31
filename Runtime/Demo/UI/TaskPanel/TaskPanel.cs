using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit;
using XiaoCaoKit.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 任务界面: 左侧分类页签, 右侧任务列表
    /// 任务条目包含描述、进度条与奖励, 右侧为领取/前往按钮(前往需配置 jumpType 才显示)
    /// </summary>
    public class TaskPanel : StandardPanel
    {
        //UIStateChangeColor 约定: 0 选中, 1 未选中
        private const int SelectState = 0;
        private const int UnSelectState = 1;

        public override UIPanelType PanelType => UIPanelType.TaskPanel;

        [Header("右侧标题栏")]
        //右侧列表顶部标题文本(如"任务类型")
        public TMP_Text titleText;

        [Header("左侧分类")]
        //分类按钮容器, 子节点第一个为模板
        public Transform categoryParent;

        [Header("右侧任务列表")]
        //任务条目容器(ScrollView 的 Content), 子节点第一个为模板
        public Transform taskParent;

        //当前选中的任务分类
        [NonSerialized] public string curCategory;

        //分类列表, 按配置出现顺序
        private readonly List<string> categories = new List<string>();

        //分类按钮上的选中态组件, 下标与 categories 一一对应
        private readonly List<UIStateChangeColor> categoryStates = new List<UIStateChangeColor>();

        //分类按钮上的红点节点(BtnRedDot), 下标与 categories 一一对应
        private readonly List<GameObject> categoryRedDots = new List<GameObject>();

        public override void Init()
        {
            if (IsInited)
            {
                return;
            }

            base.Init();
            titleText.text = "TaskPanelTitle".ToLocalizeStr();
            RefreshCategories();
            IsInited = true;
            gameObject.SetActive(false);
        }

        public override void Show(IUIData uiData = null)
        {
            if (!IsInited)
            {
                Init();
            }

            gameObject.SetActive(true);
            RefreshList();
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
            UIMgr.Inst.HideView(PanelType);
        }

        public override void OnCloseBtnClick()
        {
            Hide();
        }

        /// <summary>
        /// 按配置生成左侧分类按钮, 默认选中第一个
        /// </summary>
        private void RefreshCategories()
        {
            categories.Clear();
            categoryStates.Clear();
            categoryRedDots.Clear();
            categories.AddRange(TaskMgr.GetCategories());
            UITool.SetCellListCount(categoryParent, categories.Count);

            for (int i = 0; i < categories.Count; i++)
            {
                string category = categories[i];
                Transform cell = categoryParent.GetChild(i);
                Button btn = cell.GetComponent<Button>();
                categoryStates.Add(cell.GetComponent<UIStateChangeColor>());
                categoryRedDots.Add(cell.Find("BtnRedDot").gameObject);
                btn.GetComponentInChildren<TMP_Text>(true).text =
                    $"TaskCat_{category}".ToLocalizeStr();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectCategory(category));
            }

            if (categories.Count > 0)
            {
                SelectCategory(categories[0]);
            }
        }

        /// <summary>
        /// 切换分类并刷新任务列表, 高亮选中的分类按钮
        /// </summary>
        public void SelectCategory(string category)
        {
            curCategory = category;
            int selectedIndex = categories.IndexOf(category);
            for (int i = 0; i < categoryStates.Count; i++)
            {
                //选中态的高亮底图与文本颜色都由 UIStateChangeColor 配置
                categoryStates[i].SetState(i == selectedIndex ? SelectState : UnSelectState);
            }
            RefreshList();
        }

        /// <summary>
        /// 刷新左侧分类按钮红点: 该分类下存在可领取任务时显示
        /// </summary>
        private void RefreshCategoryRedDots()
        {
            for (int i = 0; i < categoryRedDots.Count; i++)
            {
                categoryRedDots[i].SetActive(TaskMgr.HasClaimable(categories[i]));
            }
        }

        /// <summary>
        /// 刷新右侧任务列表
        /// </summary>
        public void RefreshList()
        {
            RefreshCategoryRedDots();

            List<cfg.TaskSetting> tasks = TaskMgr.GetTasksByCategory(curCategory);
            UITool.SetCellListCount(taskParent, tasks.Count);

            for (int i = 0; i < tasks.Count; i++)
            {
                var cell = taskParent.GetChild(i).GetComponent<TaskItemCell>();
                cell.Refresh(tasks[i]);
            }
        }

        private void OnEnable()
        {
            //场景启动时可能先于 UIMgr.Init 触发, 未初始化前不监听
            if (!IsInited)
            {
                return;
            }
            UICanvasMgr.Inst.EventSystem.AddEventListener(UIEventNames.TaskChange, RefreshList);
            RefreshList();
        }

        private void OnDisable()
        {
            UICanvasMgr.Inst?.EventSystem.RemoveEventListener(UIEventNames.TaskChange, RefreshList);
        }
    }
}
