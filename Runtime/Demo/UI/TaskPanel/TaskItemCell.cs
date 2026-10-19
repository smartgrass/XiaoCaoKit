using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 任务条目: 描述 + 进度条 + 奖励文本, 右侧领取/前往按钮
    /// 领取按钮仅在可领取时显示; 前往按钮仅在任务配置了 jumpType 且未完成时显示
    /// </summary>
    public class TaskItemCell : MonoBehaviour
    {
        public TMP_Text descText;

        public TMP_Text progressText;

        public TMP_Text rewardText;

        //进度条(0~1)
        public Slider progressBar;

        //领取按钮(子节点文本为 TaskClaim)
        public Button claimBtn;

        //前往按钮(子节点文本为 TaskGoto)
        public Button gotoBtn;

        //当前展示的任务
        [NonSerialized] public cfg.TaskSetting setting;

        private void Awake()
        {
            claimBtn.onClick.AddListener(OnClaimClick);
            gotoBtn.onClick.AddListener(OnGotoClick);
        }

        /// <summary>
        /// 按任务配置刷新显示
        /// </summary>
        public void Refresh(cfg.TaskSetting taskSetting)
        {
            setting = taskSetting;

            descText.text = TaskMgr.GetDesc(setting);

            int progress = TaskMgr.GetProgress(setting);
            bool completed = progress >= setting.TargetNum;
            bool claimed = TaskMgr.IsClaimed(setting);

            //已领取时进度文本直接显示状态, 否则显示 进度/目标
            progressText.text = claimed
                ? LocalizeKey.TaskClaimed.ToLocalizeStr()
                : $"{progress}/{setting.TargetNum}";
            progressBar.value = setting.TargetNum > 0 ? (float)progress / setting.TargetNum : 0;

            RefreshRewardText();

            //领取按钮: 可领取时显示; 已领取由进度文本区展示状态
            claimBtn.gameObject.SetActive(!claimed && completed);

            //前往按钮: 需要配置跳转类型, 且任务未完成
            bool hasJump = !string.IsNullOrEmpty(setting.JumpType);
            gotoBtn.gameObject.SetActive(hasJump && !completed);
        }

        /// <summary>
        /// 奖励文本: 物品名 x 数量, 多个奖励用空格分隔
        /// </summary>
        private void RefreshRewardText()
        {
            if (setting.Reward == null || setting.Reward.Count == 0)
            {
                rewardText.text = string.Empty;
                return;
            }

            var parts = new System.Collections.Generic.List<string>();
            foreach (Item item in setting.Reward)
            {
                parts.Add($"{item.GetItemName()}x{item.num}");
            }
            rewardText.text = string.Join("  ", parts);
        }

        private void OnClaimClick()
        {
            TaskMgr.Claim(setting);
            Refresh(setting);
        }

        private void OnGotoClick()
        {
            TaskMgr.GotoTask(setting);
        }
    }
}
