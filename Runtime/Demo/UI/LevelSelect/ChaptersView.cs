using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit;

namespace XiaoCao.UI
{
    public class ChaptersView : MonoBehaviour
    {
        public Transform chapters;
        public Button backBtn;
        public Action<int> onChapterSelected;
        private readonly List<int> _allChapters = new List<int>();
        private int _currentChapter;
        private bool _isInited = false;

        private void OnBackBtnClicked()
        {
            OnSelectChapter(_currentChapter);
        }

        public void Show(int currentChapter)
        {
            if (!_isInited)
            {
                backBtn.onClick.AddListener(OnBackBtnClicked);
                _allChapters.AddRange(LubanTables.GetAllChapters());
                _allChapters.Sort();
                _isInited = true;
            }

            gameObject.SetActive(true);
            _currentChapter = currentChapter;

            InitChapterBtn();
        }

        void InitChapterBtn()
        {
            //获取tabs,所有章节名
            if (chapters == null || _allChapters.Count == 0)
            {
                Debug.Log($"-- _allChapters {_allChapters.Count}");
                return;
            }

            UITool.SetCellListCount(chapters, _allChapters.Count);
            ChapterBtn[] chapterBtns = chapters.GetComponentsInChildren<ChapterBtn>(true);
            for (int i = 0; i < _allChapters.Count && i < chapterBtns.Length; i++)
            {
                int chapter = _allChapters[i];
                var chapterBtn = chapterBtns[i];
                chapterBtn.Show(chapter);
                chapterBtn.onClick = () => { OnSelectChapter(chapter); };
            }
        }

        void OnSelectChapter(int chapter)
        {
            Debug.Log($"-- OnSelectChapter {chapter}");
            onChapterSelected?.Invoke(chapter);
            gameObject.SetActive(false);
        }
    }
}