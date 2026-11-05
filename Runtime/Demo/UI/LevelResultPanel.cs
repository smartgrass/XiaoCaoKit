using System;
using DG.Tweening;
using NaughtyAttributes;
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
        public bool closeCharacterImage;
        public Button sureBtn;
        public TMP_Text levelNameText;
        public TMP_Text killCountText;
        public TMP_Text levelTimeCountText;
        public Transform rewardParent;
        public Transform firstRewardParent;

        [Tooltip("玩家经验条, 结算时播放增长动画")]
        public ExpBar playerExpBar;

        [Tooltip("羁绊经验条, 结算时播放增长动画")]
        public ExpBar bondsExpBar;

        [Tooltip("结算文本的入场滑入动画, 面板显示时播放")]
        public LevelEndTextAnim levelEndTextAnim;

        [Header("经验条增长动画")]
        [Tooltip("经验条从空涨满所需时长(秒), 单段实际时长按增长比例缩放")]
        [Min(0f)]
        public float expGrowFullDuration = 0.8f;

        [Tooltip("单段经验增长的最短时长(秒), 避免只加 1 点经验时条几乎瞬间跳变")]
        [Min(0f)]
        public float expGrowMinDuration = 0.15f;

        [Tooltip("经验条开始增长前的延迟(秒), 让结算面板先亮完奖励再涨条")]
        [Min(0f)]
        public float expGrowStartDelay = 0.3f;

        [Header("调试")]
        [Tooltip("测试播放时玩家经验条的起始等级, 0 表示取当前存档等级")]
        public int debugExpLevel = 0;

        /// <summary>调试播放时, 升级后那一级涨到的比例</summary>
        private const float DebugGrowRatio = 0.4f;

        public override UIPanelType PanelType => UIPanelType.LevelResultPanel;

        public override void Show(IUIData data = null)
        {
            ShowUI();
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 空格键等同于点击确认按钮。
        /// </summary>
        public override void InputKeyCode(KeyCode key)
        {
            if (key == XCInputSetting.Space && sureBtn.interactable)
            {
                OnSureBtn();
            }
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
            string levelShowName = levelData.GetLevelNameText;
            int killCount = levelData.killCount;
            float timeCount = levelData.finishLevelTime - levelData.enterLevelTime;
            Show(isSuccess, levelShowName, killCount, timeCount);
            ReadRoleImg();

            //通关时播放结算音效(失败结算不播)
            if (isSuccess)
            {
                SoundMgr.Inst.PlayLevelWin();
            }

            //记录结算前的等级与经验, 发完奖励后用于播放经验条增长动画
            PlayerSaveData saveData = PlayerSaveData.LocalSavaData;
            int playerLvFrom = saveData.lv;
            int playerExpFrom = saveData.exp;
            int bondLvFrom = SupportRoleSetting.GetBondLevel();
            int bondExpFrom = saveData.bondExp;

            //获取奖励
            if (isSuccess)
            {
                GetLevelFinishReward(levelData.LevelName);
                //关卡模式通关一次获得 1 点羁绊经验, 满经验自动提升羁绊等级
                saveData.AddBondExp(SupportRoleSetting.BondExpPerLevelClear);
            }

            PlayExpGrow(playerLvFrom, playerExpFrom, bondLvFrom, bondExpFrom);

            //结算文本依次滑入
            levelEndTextAnim.Play();
        }

        /// <summary>
        /// 播放玩家经验与羁绊经验的增长动画, 数值没有变化时只刷新终值。
        /// </summary>
        private void PlayExpGrow(int playerLvFrom, int playerExpFrom, int bondLvFrom, int bondExpFrom)
        {
            PlayerSaveData saveData = PlayerSaveData.LocalSavaData;

            //满级后经验条按"已满级"显示, 不再换算升级需求
            Func<int, int> playerNeed = level => level >= GameSetting.MaxRoleLevel
                ? 0
                : CharacterAttrCalc.GetRoleNextLevelExp(level);

            playerExpBar.SetLevel(playerLvFrom, LocalizeKey.PlayerLevelShow);
            playerExpBar.SetExp(playerExpFrom, playerNeed(playerLvFrom));
            BuildExpSequence(playerExpBar, playerLvFrom, playerExpFrom, saveData.lv, saveData.exp,
                    playerNeed,
                    level => playerExpBar.SetLevel(level, LocalizeKey.PlayerLevelShow))
                .Play();

            bondsExpBar.SetLevel(bondLvFrom, LocalizeKey.BondsTitleLevel);
            bondsExpBar.SetExp(bondExpFrom, SupportRoleSetting.GetNextLevelExp(bondLvFrom));
            BuildExpSequence(bondsExpBar, bondLvFrom, bondExpFrom, SupportRoleSetting.GetBondLevel(), saveData.bondExp,
                    SupportRoleSetting.GetNextLevelExp,
                    level => bondsExpBar.SetLevel(level, LocalizeKey.BondsTitleLevel))
                .Play();
        }

        /// <summary>
        /// 按"先涨满当前等级, 再逐级续接"的方式生成经验条增长序列, 最后一段停在结算后的经验上。
        /// </summary>
        private Sequence BuildExpSequence(ExpBar bar, int fromLv, int fromExp, int toLv, int toExp,
            Func<int, int> getNeed, Action<int> onLevelChange)
        {
            Sequence sequence = DOTween.Sequence();
            int level = fromLv;
            int exp = fromExp;

            //跨级时逐级结算: 当前等级涨满 -> 切换等级文本 -> 下一级从 0 继续涨
            while (level < toLv)
            {
                int segLevel = level;
                int segExp = exp;
                int segNeed = getNeed(segLevel);
                sequence.Append(bar.PlayExp(segExp, segNeed, segNeed, GetGrowDuration(segNeed - segExp, segNeed)));
                sequence.AppendCallback(() =>
                {
                    int nextLevel = segLevel + 1;
                    onLevelChange(nextLevel);
                    bar.SetExp(0, getNeed(nextLevel));
                });

                level = segLevel + 1;
                exp = 0;
            }

            int lastNeed = getNeed(level);
            sequence.Append(bar.PlayExp(exp, toExp, lastNeed, GetGrowDuration(toExp - exp, lastNeed)));

            //统一在序列最前面插入延迟: 结算面板先展示奖励和终值文本, 稍等再开始涨条
            if (expGrowStartDelay > 0f)
            {
                sequence.PrependInterval(expGrowStartDelay);
            }

            return sequence;
        }

        /// <summary>
        /// 按增长比例换算单段动画时长, 涨满一整条为 expGrowFullDuration。
        /// </summary>
        private float GetGrowDuration(int expDelta, int need)
        {
            if (need <= 0 || expDelta <= 0)
            {
                return expGrowMinDuration;
            }

            return Mathf.Max(expGrowMinDuration, expGrowFullDuration * expDelta / need);
        }

        /// <summary>
        /// 调试: 播放一次经验条增长动画, 不读写存档。
        /// 两条经验条都从当前等级 0 经验涨满, 升一级后继续涨到新等级需求的 40%, 用于确认跨级切换与时长手感。
        /// </summary>
        [Button("测试播放经验条动画", enabledMode: EButtonEnableMode.Playmode)]
        public void PlayExpGrowDebug()
        {
            //连点按钮时先清掉正在播放的补间, 避免多条动画叠加
            DOTween.Kill(this);

            //玩家经验条
            int maxPlayerLv = Mathf.Max(GameSetting.MinRoleLevel, GameSetting.MaxRoleLevel);
            int playerLv = debugExpLevel > 0 ? debugExpLevel : PlayerSaveData.LocalSavaData.lv;
            //演示要跨一级, 起点最高取到满级前一级
            playerLv = Mathf.Clamp(playerLv, GameSetting.MinRoleLevel, Mathf.Max(GameSetting.MinRoleLevel, maxPlayerLv - 1));
            int playerNeed = CharacterAttrCalc.GetRoleNextLevelExp(playerLv);
            int playerNextLv = playerLv + 1;
            int playerNextNeed = playerNextLv <= GameSetting.MaxRoleLevel
                ? CharacterAttrCalc.GetRoleNextLevelExp(playerNextLv)
                : 0;
            //已经到顶就只演示涨满当前等级
            int playerToLv = playerNextNeed > 0 ? playerNextLv : playerLv;
            int playerToExp = playerNextNeed > 0 ? Mathf.RoundToInt(playerNextNeed * DebugGrowRatio) : playerNeed;

            playerExpBar.SetLevel(playerLv, LocalizeKey.PlayerLevelShow);
            playerExpBar.SetExp(0, playerNeed);
            BuildExpSequence(playerExpBar, playerLv, 0, playerToLv, playerToExp,
                    CharacterAttrCalc.GetRoleNextLevelExp,
                    level => playerExpBar.SetLevel(level, LocalizeKey.PlayerLevelShow))
                .Play();

            //羁绊经验条
            int maxBondLv = SupportRoleSetting.MaxBondLevel;
            int bondLv = Mathf.Clamp(SupportRoleSetting.GetBondLevel(), 1, Mathf.Max(1, maxBondLv - 1));
            int bondNeed = SupportRoleSetting.GetNextLevelExp(bondLv);
            int bondNextLv = bondLv + 1;
            int bondNextNeed = bondNextLv <= maxBondLv ? SupportRoleSetting.GetNextLevelExp(bondNextLv) : 0;
            int bondToLv = bondNextNeed > 0 ? bondNextLv : bondLv;
            int bondToExp = bondNextNeed > 0 ? Mathf.RoundToInt(bondNextNeed * DebugGrowRatio) : bondNeed;

            bondsExpBar.SetLevel(bondLv, LocalizeKey.BondsTitleLevel);
            bondsExpBar.SetExp(0, bondNeed);
            BuildExpSequence(bondsExpBar, bondLv, 0, bondToLv, bondToExp,
                    SupportRoleSetting.GetNextLevelExp,
                    level => bondsExpBar.SetLevel(level, LocalizeKey.BondsTitleLevel))
                .Play();
        }

        //通关奖励
        private void GetLevelFinishReward(string levelName)
        {
            var list = LevelSettingHelper.GetReward(levelName);
            foreach (Item item in list)
            {
                var rewardItem = Instantiate(UIPrefabSo.Inst.itemCellPrefab, rewardParent);
                item.RewardItem();
                rewardItem.GetComponent<ItemCell>().SetItem(item);
            }

            //
            var info = LevelInfo.ParseString(levelName);
            bool hasFirstReward =
                PlayerSaveData.LocalSavaData.levelPassData.HasGetFirstReward(info.chapter, info.index);
            var firstRewardList = LevelSettingHelper.GetFirstReward(levelName);

            //如果hasFirstReward

            foreach (Item item in firstRewardList)
            {
                var rewardItem = Instantiate(UIPrefabSo.Inst.itemCellPrefab, firstRewardParent);
                if (!hasFirstReward)
                {
                    item.RewardItem();
                }

                var cell = rewardItem.GetComponent<ItemCell>().SetItem(item);

                if (hasFirstReward)
                {
                    cell.SetMask("hasGet");
                }
            }

            if (!hasFirstReward)
            {
                PlayerSaveData.LocalSavaData.levelPassData.SetHasGetFirstReward(info.chapter, info.index);
            }

            //结算奖励后立即存盘, 防止奖励只改了内存没写入存档
            PlayerSaveData.SavaData();
        }


        private void Show(bool isSuccess, string levelName, int killCount, float timeCount)
        {
            levelNameText.text = levelName;
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
            if (closeCharacterImage)
            {
                return;
            }
            characterImage.ChangeModelKey(BattleData.Current.curBodyName);
        }
    }
}
