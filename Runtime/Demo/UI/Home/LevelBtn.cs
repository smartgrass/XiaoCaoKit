using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCaoKit;
using XiaoCaoKit.UI;

namespace XiaoCao.UI
{
    public class LevelBtn : MonoBehaviour
    {
        public TMP_Text titleText;
        public Button btn;
        public UIStateChange stateChange;
        public Action onClick;
        public Transform rewardParent;


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
            UpdateReward();
        }

        private LevelPassState GetPassState(int chapter, int index)
        {
            return PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, index);
        }

        private void UpdateReward()
        {
            if (rewardParent == null)
            {
                return;
            }

            string levelKey = MapNames.GetLevelKey(curChapter, levelIndex);
            var rewards = LevelSettingHelper.GetReward(levelKey);
            if (rewards == null)
            {
                UITool.SetCellListCount(rewardParent, 0);
                return;
            }

            UITool.SetCellListCount(rewardParent, rewards.Count);
            var cells = rewardParent.GetComponentsInChildren<ItemCell>(false);
            for (int i = 0; i < rewards.Count && i < cells.Length; i++)
            {
                cells[i].SetItem(rewards[i]);
            }
        }
    }
}
