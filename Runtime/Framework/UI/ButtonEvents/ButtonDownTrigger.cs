using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XiaoCao;

/// <summary>
/// 按钮触发时机切换: 开启 <see cref="GameSetting.BtnTriggerOnDown"/> 时在 PointerDown 立刻触发 onTrigger,
/// 并忽略抬手产生的 Button.onClick; 关闭时与普通 Button 一样只在抬起时触发
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonDownTrigger : MonoBehaviour, IPointerDownHandler
{
    /// <summary>按钮触发回调, 按下/抬起两种时机共用同一次回调</summary>
    public UnityEvent onTrigger = new UnityEvent();

    private Button _button;

    private void Awake()
    {
        EnsureEventsInitialized();
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnButtonClick);
        }
    }

    /// <summary>
    /// 保证回调非空: Inspector 上被清掉引用或运行时先 AddListener 后 Awake 都能正常使用
    /// </summary>
    public void EnsureEventsInitialized()
    {
        if (onTrigger == null)
        {
            onTrigger = new UnityEvent();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!GameSetting.BtnTriggerOnDown || !_button.IsInteractable())
        {
            return;
        }

        onTrigger.Invoke();
    }

    /// <summary>
    /// 普通 Button 的抬起点击: 按下触发模式下这次点击已经提前消费过, 直接跳过
    /// </summary>
    private void OnButtonClick()
    {
        if (GameSetting.BtnTriggerOnDown)
        {
            return;
        }

        onTrigger.Invoke();
    }
}
