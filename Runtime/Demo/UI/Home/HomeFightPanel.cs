using System;
using System.Collections.Generic;
using cfg;
using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XiaoCao;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    public class HomeFightPanel : HomePanelBase
    {
        public Transform levels;
        public Transform tabs;
        public LevelDetailUI levelDetailUI;

        public Button backBtn;

        public int curChapter = 0;

        [Header("Level Button Hover")]
        public float hoverScale = 1.05f;
        public float hoverTweenDuration = 0.08f;
        public Ease hoverEase = Ease.OutQuad;

        private LevelBtn _selectedLevelBtn;
        private readonly Dictionary<LevelBtn, Vector3> _levelBtnBaseScales = new Dictionary<LevelBtn, Vector3>();

        private void Start()
        {
            backBtn.onClick.AddListener(() => { HomeHud.Inst.SwitchPanel(EHomePanel.MainPanel); });

            InitChapterBtn();

            ShowChapterView(0);
        }

        void InitChapterBtn()
        {
            //获取tabs,所有章节名
            var allChapters = LubanTables.GetAllChapters();
            UITool.SetCellListCount(tabs, allChapters.Count);
            ChapterBtn[] chapterBtns = tabs.GetComponentsInChildren<ChapterBtn>();
            for (int i = 0; i < allChapters.Count; i++)
            {
                var chapterBtn = chapterBtns[i];
                chapterBtn.Show(allChapters[i]);
                chapterBtn.onClick += () =>
                {
                    ShowChapterView(chapterBtn.curChapter);
                };
            }
        }


        void ShowChapterView(int chapter)
        {
            curChapter = chapter;
            //获取Chapter的关卡
            var chapterSetting = LubanTables.GetChapterSetting(chapter);
            // chapterSetting.Levels;

            UITool.SetCellListCount(levels, chapterSetting.Levels.Count);
            LevelBtn[] levelBtns = levels.GetComponentsInChildren<LevelBtn>();
            for (int i = 0; i < levelBtns.Length; i++)
            {
                var levelBtn = levelBtns[i];
                levelBtn.Show(chapterSetting.Id, chapterSetting.Levels[i]);
                levelBtn.onClick = null;
                levelBtn.onClick += () =>
                {
                    SetSelectedLevelBtn(levelBtn);
                    levelDetailUI.Show(levelBtn.curChapter, levelBtn.levelIndex);
                };
                ConfigureLevelBtn(levelBtn);
            }

            // LevelUIInfo levelUIInfo = levelUISettingSo.GetOrDefault(chapter);
        }

        private void OnClickLevel(int index)
        {
            // SelectLevel = MapNames.GetLevelKey(curChapter, index);
            // levelDetailUI.Show();
        }

        private void ConfigureLevelBtn(LevelBtn levelBtn)
        {
            if (levelBtn == null || levelBtn.btn == null)
            {
                return;
            }

            CacheLevelBtnScale(levelBtn);
            if (_selectedLevelBtn != levelBtn)
            {
                TweenLevelBtnScale(levelBtn, false);
            }

            EventTrigger trigger = levelBtn.btn.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = levelBtn.btn.gameObject.AddComponent<EventTrigger>();
            }

            if (trigger.triggers == null)
            {
                trigger.triggers = new List<EventTrigger.Entry>();
            }
            else
            {
                trigger.triggers.Clear();
            }

            AddEventTrigger(trigger, EventTriggerType.PointerEnter, _ => OnLevelBtnHover(levelBtn, true));
            AddEventTrigger(trigger, EventTriggerType.PointerExit, _ => OnLevelBtnHover(levelBtn, false));
        }

        private void OnLevelBtnHover(LevelBtn levelBtn, bool isHovering)
        {
            if (levelBtn == null || levelBtn.btn == null || !levelBtn.btn.interactable)
            {
                return;
            }

            if (_selectedLevelBtn == levelBtn)
            {
                return;
            }

            TweenLevelBtnScale(levelBtn, isHovering);
        }

        private void SetSelectedLevelBtn(LevelBtn levelBtn)
        {
            if (_selectedLevelBtn == levelBtn)
            {
                return;
            }

            if (_selectedLevelBtn != null)
            {
                TweenLevelBtnScale(_selectedLevelBtn, false);
            }

            _selectedLevelBtn = levelBtn;

            if (_selectedLevelBtn != null)
            {
                TweenLevelBtnScale(_selectedLevelBtn, true);
            }
        }

        private void CacheLevelBtnScale(LevelBtn levelBtn)
        {
            if (!_levelBtnBaseScales.ContainsKey(levelBtn))
            {
                _levelBtnBaseScales[levelBtn] = levelBtn.transform.localScale;
            }
        }

        private void TweenLevelBtnScale(LevelBtn levelBtn, bool isHovering)
        {
            if (levelBtn == null)
            {
                return;
            }

            CacheLevelBtnScale(levelBtn);
            Vector3 baseScale = _levelBtnBaseScales[levelBtn];
            Vector3 targetScale = isHovering ? baseScale * hoverScale : baseScale;
            levelBtn.transform.DOKill();
            levelBtn.transform.DOScale(targetScale, hoverTweenDuration).SetEase(hoverEase);
        }

        private static void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityAction<BaseEventData> action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }
    }
}
