/*--------------------------------------
   Email  : hamza95herbou@gmail.com
   Github : https://github.com/herbou
----------------------------------------*/

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonLongPressListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Tooltip("Hold duration in seconds")]
    [Range(0.3f, 5f)] public float holdDuration = 0.5f;
    public UnityEvent onLongPress = new UnityEvent();
    public UnityEvent<PointerEventData> onDrag = new UnityEvent<PointerEventData>();
    public UnityEvent<PointerEventData> onPointerUp = new UnityEvent<PointerEventData>();

    private bool isPointerDown = false;
    private bool isLongPressed = false;
    private float pressStartTime;

    private Button button;
    private Coroutine timerCoroutine;

    private void Awake()
    {
        EnsureEventsInitialized();
        button = GetComponent<Button>();
    }

    private void OnValidate()
    {
        EnsureEventsInitialized();
    }

    private void OnDisable()
    {
        StopPressTracking();
    }

    public bool IsPointerDown => isPointerDown;

    public bool IsLongPressed => isLongPressed;

    public void EnsureEventsInitialized()
    {
        if (onLongPress == null)
        {
            onLongPress = new UnityEvent();
        }

        if (onDrag == null)
        {
            onDrag = new UnityEvent<PointerEventData>();
        }

        if (onPointerUp == null)
        {
            onPointerUp = new UnityEvent<PointerEventData>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopTimerCoroutine();
        isPointerDown = true;
        isLongPressed = false;
        pressStartTime = Time.unscaledTime;
        Debug.Log($"[ButtonLongPressListener] PointerDown name={gameObject.name}, frame={Time.frameCount}, holdDuration={holdDuration:F2}, interactable={(button != null && button.interactable)}, pos={(eventData != null ? eventData.position.ToString() : "null")}");
        timerCoroutine = StartCoroutine(Timer());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        float holdTime = Time.unscaledTime - pressStartTime;
        Debug.Log($"[ButtonLongPressListener] PointerUp name={gameObject.name}, frame={Time.frameCount}, holdTime={holdTime:F3}, isLongPressed={isLongPressed}, pos={(eventData != null ? eventData.position.ToString() : "null")}");
        onPointerUp?.Invoke(eventData);
        StopPressTracking();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPointerDown)
        {
            onDrag?.Invoke(eventData);
        }
    }

    private IEnumerator Timer()
    {
        while (isPointerDown && !isLongPressed)
        {
            if (Time.unscaledTime - pressStartTime >= holdDuration)
            {
                isLongPressed = true;
                if (button.interactable)
                {
                    onLongPress?.Invoke();
                }

                timerCoroutine = null;
                yield break;
            }

            yield return null;
        }

        timerCoroutine = null;
    }

    private void StopPressTracking()
    {
        StopTimerCoroutine();
        isPointerDown = false;
        isLongPressed = false;
    }

    private void StopTimerCoroutine()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }
}
