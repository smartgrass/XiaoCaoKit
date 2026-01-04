using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    public class ChapterBtn : MonoBehaviour
    {
        public TMP_Text titleText;
        public Button btn;
        public int curChapter;

        public Action onClick;


        private void Awake()
        {
            btn.onClick.AddListener(() => { onClick?.Invoke(); });
        }

        public void Show(int chapter)
        {
            LevelPassState passState = GetPassState(chapter, 0);
            titleText.text = LocalizeKey.GetChapterName(chapter);
            btn.interactable = passState != LevelPassState.Lock;
            curChapter = chapter;
        }

        private LevelPassState GetPassState(int chapter, int index)
        {
            return PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, index);
        }
    }
}