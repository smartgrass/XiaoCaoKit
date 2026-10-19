using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    /// <summary>
    /// 任务界面: 左侧分类页签, 右侧任务列表
    /// 任务条目包含描述、进度条与奖励, 右侧为领取/前往按钮(前往需配置 jumpType 才显示)
    /// </summary>
    public class TaskPanel : StandardPanel
    {
        public override UIPanelType PanelType => UIPanelType.TaskPanel;

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

        public override void Init()
        {
            if (IsInited)
            {
                return;
            }

            base.Init();
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
            categories.AddRange(TaskMgr.GetCategories());
            UITool.SetCellListCount(categoryParent, categories.Count);

            for (int i = 0; i < categories.Count; i++)
            {
                string category = categories[i];
                Button btn = categoryParent.GetChild(i).GetComponent<Button>();
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
            for (int i = 0; i < categoryParent.childCount; i++)
            {
                var child = categoryParent.GetChild(i);
                bool selected = categories.IndexOf(category) == i;
                //选中态用颜色区分, 未选中置灰
                var graphic = child.GetComponent<Image>();
                if (graphic != null)
                {
                    graphic.color = selected ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
                }
            }
            RefreshList();
        }

        /// <summary>
        /// 刷新右侧任务列表
        /// </summary>
        public void RefreshList()
        {
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
