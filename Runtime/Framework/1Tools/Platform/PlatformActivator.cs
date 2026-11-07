using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

/// <summary>
/// 平台显隐组件：命中目标平台时激活目标物体，其余平台按配置隐藏。
/// 默认目标平台为 PC；未指定目标物体时作用于本组件所在节点。
/// </summary>
public class PlatformActivator : MonoBehaviour
{
    /// <summary>目标平台，可同时勾选 PC 与 Mobile。</summary>
    [System.Flags]
    public enum ETargetPlatform
    {
        None = 0,
        PC = 1 << 0,
        Mobile = 1 << 1,
        All = PC | Mobile,
    }

    [Tooltip("命中哪些平台时激活物体，默认 PC")]
    [SerializeField] private ETargetPlatform targetPlatform = ETargetPlatform.PC;

    [Tooltip("要激活的物体，留空表示本组件所在节点")]
    [SerializeField] private GameObject targetObject;

    [Tooltip("未命中平台时是否隐藏物体；关闭则只做激活、不做隐藏")]
    [SerializeField] private bool hideOnOtherPlatform = true;

    private void Awake()
    {
        Apply();
    }

    /// <summary>按当前平台应用激活状态，运行时也可手动调用。</summary>
    public void Apply()
    {
        GameObject go = targetObject ? targetObject : gameObject;

        if ((targetPlatform & CurrentPlatform) != 0)
        {
            go.SetActive(true);
        }
        else if (hideOnOtherPlatform)
        {
            go.SetActive(false);
        }
    }

    /// <summary>当前运行平台对应的枚举值，移动端以项目统一口径 DebugSetting 为准。</summary>
    public static ETargetPlatform CurrentPlatform =>
        DebugSetting.IsMobileOffice ? ETargetPlatform.Mobile : ETargetPlatform.PC;

    [Button("立即应用")]
    private void ApplyInEditor()
    {
        Apply();
    }
}
