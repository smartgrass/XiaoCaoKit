using UnityEngine;

/// <summary>
/// 让挂载所在的 UI 节点铺满整个 Canvas(不改变父子层级), 并在分辨率/画布尺寸变化时自动重算。
/// 适合放在被别的父节点缩放过、但仍要覆盖整屏的背景遮罩上。
/// </summary>
public class UIFullScreenOnEnable : MonoBehaviour
{
    private RectTransform _rt;
    private RectTransform _parentRt;
    private Canvas _canvas;
    private RectTransform _canvasRt;

    //上一次铺满时的尺寸特征, 变了才重算
    private Vector2 _lastCanvasSize;
    private Vector2 _lastParentSize;
    private Vector3 _lastScale;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void OnEnable()
    {
        SetFillCanvasWithoutReparent();
    }

    private void Update()
    {
        if (IsCanvasSizeChanged())
        {
            SetFillCanvasWithoutReparent();
        }
    }

    /// <summary>
    /// 把自身 RectTransform 的 offset 改成覆盖整个 Canvas, 并在父空间里换算, 保证不受父级缩放影响。
    /// </summary>
    public void SetFillCanvasWithoutReparent()
    {
        //每次铺满前重新取一遍引用, 保证被换过父节点后仍然正确
        CacheTransform();

        if (_rt == null || _parentRt == null)
        {
            return;
        }

        if (_canvas == null || _canvasRt == null)
        {
            Debug.LogError("目标UI不在Canvas下！", this);
            return;
        }

        // 获取Canvas屏幕矩形
        Rect canvasRect = _canvasRt.rect;

        // 将canvas的矩形，转换到目标rect的父空间，然后赋值offset
        Vector2 canvasMin = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, _canvasRt.TransformPoint(canvasRect.min));
        Vector2 canvasMax = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, _canvasRt.TransformPoint(canvasRect.max));

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRt, canvasMin, _canvas.worldCamera, out Vector2 localMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRt, canvasMax, _canvas.worldCamera, out Vector2 localMax);

        _rt.anchorMin = new Vector2(0.5f, 0.5f);
        _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.offsetMin = localMin;
        _rt.offsetMax = localMax;

        RecordSize();
    }

    /// <summary>缓存节点关系, 避免每帧在层级里找 Canvas</summary>
    private void CacheTransform()
    {
        _rt = transform as RectTransform;
        _parentRt = _rt != null ? _rt.parent as RectTransform : null;
        _canvas = _rt != null ? _rt.GetComponentInParent<Canvas>() : null;
        _canvasRt = _canvas != null ? _canvas.transform as RectTransform : null;
    }

    /// <summary>画布尺寸、父级尺寸、缩放或屏幕分辨率变化时返回 true</summary>
    private bool IsCanvasSizeChanged()
    {
        if (_canvasRt == null)
        {
            return false;
        }

        Vector2 canvasSize = _canvasRt.rect.size;
        Vector2 parentSize = _parentRt != null ? _parentRt.rect.size : Vector2.zero;
        Vector3 scale = _rt != null ? _rt.lossyScale : Vector3.one;

        return canvasSize != _lastCanvasSize
               || parentSize != _lastParentSize
               || scale != _lastScale
               || Screen.width != _lastScreenWidth
               || Screen.height != _lastScreenHeight;
    }

    private void RecordSize()
    {
        _lastCanvasSize = _canvasRt != null ? _canvasRt.rect.size : Vector2.zero;
        _lastParentSize = _parentRt != null ? _parentRt.rect.size : Vector2.zero;
        _lastScale = _rt != null ? _rt.lossyScale : Vector3.one;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }
}
