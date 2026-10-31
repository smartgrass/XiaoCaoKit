using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 任务条目: 左侧图标 + 任务描述, 下方为进度条(<see cref="ExpBar"/>, 含进度数值文本),
    /// 右侧为领取/前往按钮
    /// 领取与前往按钮互斥: 完成后显示领取(已领取则都不显示), 未完成且配置了 jumpType 时显示前往
    /// 已领取时整条显示 <see cref="reward"/> 覆盖层(半透明遮罩 + 已领取对勾)
    /// </summary>
    public class TaskItemCell : MonoBehaviour
    {
        public TMP_Text descText;

        //已领取覆盖层: 整条遮罩 + 对勾, 仅在已领取时显示
        public GameObject reward;

        //进度条(0~1): 复用经验条控件, 填充与进度数值文本都由 ExpBar 负责
        public ExpBar progressBar;

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

            //已领取时数值区显示状态文本, 否则显示 进度/目标
            string progressStr = claimed ? LocalizeKey.TaskClaimed.ToLocalizeStr() : $"{progress}/{setting.TargetNum}";
            progressBar.SetProgress(setting.TargetNum > 0 ? (float)progress / setting.TargetNum : 0f, progressStr);

            //已领取后整条打上覆盖层(遮罩 + 对勾)
            reward.SetActive(claimed);

            //两个按钮互斥: 任务完成后只显示领取按钮(已领取则都不显示), 否则只显示前往按钮
            if (completed)
            {
                SetBtnVisible(claimBtn, !claimed);
                SetBtnVisible(gotoBtn, false);
            }
            else
            {
                SetBtnVisible(claimBtn, false);
                //未完成时才引导前往: 未配置 jumpType 的任务没有可跳转的目标
                SetBtnVisible(gotoBtn, !string.IsNullOrEmpty(setting.JumpType));
            }
        }

        /// <summary>
        /// 按钮显隐: 按钮预制体(YellowBtn/BlueBtn)把视觉放在根节点, Button 挂在内层 Btn 子节点,
        /// 只关 Button 自身会留下底图和文字, 所以这里取该按钮在条目下的整棵节点再显隐
        /// </summary>
        private void SetBtnVisible(Button btn, bool visible)
        {
            Transform node = btn.transform;
            while (node.parent != null && node.parent != transform)
            {
                node = node.parent;
            }
            node.gameObject.SetActive(visible);
        }

        private void OnClaimClick()
        {
            //领取后任务会沉底, 条目次序会变, 所以交给 TaskChange 事件统一刷新列表,
            //不要在这里再 Refresh 自身(这个条目可能已经被换成别的任务了)
            TaskMgr.Claim(setting);
        }

        private void OnGotoClick()
        {
            TaskMgr.GotoTask(setting);
        }
    }
}
