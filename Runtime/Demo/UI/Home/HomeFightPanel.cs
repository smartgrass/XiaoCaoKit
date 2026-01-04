using System;
using cfg;
using NaughtyAttributes;
using TMPro;
using UnityEditor;
using UnityEngine;
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
                levelBtn.onClick += () => { levelDetailUI.Show(levelBtn.curChapter, levelBtn.levelIndex); };
            }

            // LevelUIInfo levelUIInfo = levelUISettingSo.GetOrDefault(chapter);
        }

        private void OnClickLevel(int index)
        {
            // SelectLevel = MapNames.GetLevelKey(curChapter, index);
            // levelDetailUI.Show();
        }
    }
}