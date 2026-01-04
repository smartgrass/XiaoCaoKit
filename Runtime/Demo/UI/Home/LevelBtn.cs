using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit.UI;

namespace XiaoCao.UI
{
    public class LevelBtn : MonoBehaviour
    {
        public TMP_Text titleText;
        public Button btn;
        public UIStateChange stateChange;
        public Action onClick;


        public int curChapter;
        public int levelIndex;


        private void Awake()
        {
            btn.onClick.AddListener(() => { onClick?.Invoke(); });
        }

        public void Show(int chapter, int index)
        {
            this.curChapter = chapter;
            this.levelIndex = index;
            titleText.text = LocalizeKey.GetLevelName(chapter, index);


            LevelPassState passState = GetPassState(chapter, index);
            btn.interactable = passState != LevelPassState.Lock;
            stateChange.SetState((int)passState);
        }

        private LevelPassState GetPassState(int chapter, int index)
        {
            return PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, index);
        }
    }
}