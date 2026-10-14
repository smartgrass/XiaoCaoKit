using UnityEngine;

public class UIFullScreenOnEnable : MonoBehaviour
{
    public RectTransform targetRect;

    private void OnEnable()
    {
        SetFillCanvasWithoutReparent();
    }
    
    void SetFillCanvasWithoutReparent()
    {
        RectTransform rt = targetRect ?? GetComponent<RectTransform>();
        if (rt == null) return;

        Canvas canvas = rt.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("目标UI不在Canvas下！", this);
            return;
        }
        RectTransform canvasRt = canvas.GetComponent<RectTransform>();

        // 获取Canvas屏幕矩形
        Rect canvasRect = canvasRt.rect;

        // 将canvas的矩形，转换到目标rect的父空间，然后赋值offset
        RectTransform parentRt = rt.parent as RectTransform;
        if (parentRt == null) return;

        Vector2 canvasMin = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, canvasRt.TransformPoint(canvasRect.min));
        Vector2 canvasMax = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, canvasRt.TransformPoint(canvasRect.max));

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, canvasMin, canvas.worldCamera, out Vector2 localMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, canvasMax, canvas.worldCamera, out Vector2 localMax);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.offsetMin = localMin;
        rt.offsetMax = localMax;
    }
}
