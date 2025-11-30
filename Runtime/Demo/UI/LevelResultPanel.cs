using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit
{
    public class LevelResultPanel : PanelBase
    {
        public CharacterImage characterImage;
        public TMP_Text titleText;
        public Button sureBtn;
        public TMP_Text levelNameText;
        public TMP_Text killCountText;
        public TMP_Text levelTimeCountText;
        public Transform rewardParent;
        public override UIPanelType PanelType => UIPanelType.LevelResultPanel;

        public override void Show(IUIData data = null)
        {
            ShowUI();
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnSureBtn()
        {
            //由于是直接切换场景, UIMgr会被销毁,不需要调用Hide
            GameMgr.Inst.BackHome();
        }

        private void Start()
        {
            sureBtn.onClick.AddListener(OnSureBtn);
        }

        private void ShowUI()
        {
            if (gameObject.activeSelf)
            {
                return;
            }

            gameObject.SetActive(true);
            LevelData levelData = LevelData.Current;
            bool isSuccess = levelData.levelResult == ELevelResult.Success;
            string levelName = levelData.GetLevelNameText;
            int killCount = levelData.killCount;
            float timeCount = levelData.finishLevelTime - levelData.enterLevelTime;
            Show(isSuccess, levelName, killCount, timeCount);
            ReadRoleImg();
            //获取奖励
            if (isSuccess)
            {
                GetLevelFinishReward(levelName);
            }
        }

        private void GetLevelFinishReward(string levelName)
        {
            var list = LevelSettingHelper.GetReward(levelName);
            foreach (Item item in list)
            {
                var rewardItem = Instantiate(UIPrefabSo.Inst.itemCellPrefab, rewardParent);
                item.RewardItem();
                rewardItem.GetComponent<ItemCell>().SetItem(item);
            }
        }


        private void Show(bool isSuccess, string levelName, int killCount, float timeCount)
        {
            levelNameText.text = levelName;
            titleText.text =
                isSuccess ? LocalizeKey.LevelSuccess.ToLocalizeStr() : LocalizeKey.LevelFail.ToLocalizeStr();
            killCountText.text = $"{LocalizeKey.KillCount.ToLocalizeStr()}: {killCount}";
            //时间 分:秒
            int minutes = (int)(timeCount / 60);
            int seconds = (int)(timeCount % 60);
            string timeText = $"{minutes:D2}:{seconds:D2}";
            levelTimeCountText.text = $"{LocalizeKey.LevelTimeCount.ToLocalizeStr()}: {timeText}";
            gameObject.SetActive(true);
        }

        public void ReadRoleImg()
        {
            characterImage.ChangeModelKey(BattleData.Current.curBodyName);
        }
    }
}