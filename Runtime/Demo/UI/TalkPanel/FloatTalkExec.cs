using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

/// <summary>
/// 对话出现执行器: 在固定位置浮出一句对话气泡。
/// <para>调用 <see cref="FloatTalkPanel"/>, 只做"显示一句话"这一件事, 不打断玩家操作, 也不进 TalkPanel 流程。</para>
/// <para>默认开局自动执行; 不需要自动播就把 isExecuteOnStart 取消勾选, 由触发器 / MapMsg 调 <see cref="Execute"/></para>
/// </summary>
public class FloatTalkExec : GameStartMonoExec
{
    [Label("角色")]
    [Tooltip("角色标识, 只用来取头像: 场景节点名(npc01) / 角色模型名(Role_1) / ShowAct 里的标记名")]
    public string roleName = "Role_1";

    [Label("对话Key")]
    [Tooltip("本地化 key; 找不到配置时气泡里会显示黄色的 key 本身, 方便发现漏配")]
    public string talkKey;

    [Label("出现持续时间(秒)")]
    [Tooltip("完全显示后的停留时间, 不含淡入淡出; 总时长 = 淡入 + 持续 + 淡出")]
    [MinValue(0f)]
    public float duration = 2f;

    [Label("淡入时间(秒)")]
    [Tooltip("0 = 直接出现, 不做淡入")]
    [MinValue(0f)]
    public float fadeIn = 0.2f;

    [Label("淡出时间(秒)")]
    [Tooltip("0 = 到点直接消失, 不做淡出")]
    [MinValue(0f)]
    public float fadeOut = 0.3f;

    [Label("启动延迟(秒)")]
    [Tooltip("触发起等多久再显示气泡; 0 = 立刻显示。延迟计时不受暂停/慢放影响")]
    [MinValue(0f)]
    public float startDelay;

    /// <summary>延迟中的计时器, 重复触发时先取消上一个</summary>
    private Timer delayTimer;

    public override void Execute()
    {
        if (string.IsNullOrEmpty(talkKey))
        {
            Debug.LogWarning($"--- {name} 没有填对话Key, 不显示气泡");
            return;
        }

        CancelDelay();

        if (startDelay <= 0f)
        {
            ShowTalk();
            return;
        }

        delayTimer = XCTime.DelayTimer(startDelay, OnDelayEnd);
    }

    private void OnDelayEnd()
    {
        delayTimer = null;
        //延迟期间组件/节点被销毁或禁用时不再显示
        if (this == null || !isActiveAndEnabled)
        {
            return;
        }

        ShowTalk();
    }

    private void ShowTalk()
    {
        FloatTalkPanel.ShowTalk(roleName, talkKey, duration, fadeIn, fadeOut);
    }

    private void CancelDelay()
    {
        if (delayTimer != null)
        {
            delayTimer.Destroy();
            delayTimer = null;
        }
    }

    /// <summary>在编辑器里立刻播一次(忽略启动延迟, 仅 Play 模式), 方便调台词和节奏</summary>
    [Button("播放对话", enabledMode: EButtonEnableMode.Playmode)]
    private void ExecuteByButton()
    {
        if (string.IsNullOrEmpty(talkKey))
        {
            Debug.LogWarning($"--- {name} 没有填对话Key, 不显示气泡");
            return;
        }

        CancelDelay();
        ShowTalk();
    }

    /// <summary>立刻收起气泡(延迟还没走完也会一起取消)</summary>
    [Button("收起对话", enabledMode: EButtonEnableMode.Playmode)]
    private void HideByButton()
    {
        CancelDelay();
        FloatTalkPanel.Hide();
    }

    private void OnDisable()
    {
        CancelDelay();
    }
}
