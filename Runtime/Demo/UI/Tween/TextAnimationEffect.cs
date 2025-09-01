using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// TextMeshPro文字动画效果组件
/// 实现文字的颜色和缩放循环变化动画
/// </summary>
public class TextAnimationEffect : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("动画持续时间")]
    public float duration = 2f;
    
    [Tooltip("是否循环播放")]
    public bool loop = true;
    
    [Header("颜色变化")]
    [Tooltip("起始颜色")]
    public Color startColor = Color.white;
    
    [Tooltip("结束颜色")]
    public Color endColor = Color.red;
    
    [Header("缩放变化")]
    [Tooltip("起始缩放")]
    public Vector3 startScale = Vector3.one;
    
    [Tooltip("结束缩放")]
    public Vector3 endScale = Vector3.one * 1.5f;
    
    [Header("动画曲线")]
    [Tooltip("颜色变化曲线")]
    public AnimationCurve colorCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Tooltip("缩放变化曲线")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private TextMeshProUGUI textMeshPro;
    private Coroutine animationCoroutine;
    
    void Awake()
    {
        // 获取TextMeshProUGUI组件
        textMeshPro = GetComponent<TextMeshProUGUI>();
        if (textMeshPro == null)
        {
            textMeshPro = gameObject.AddComponent<TextMeshProUGUI>();
        }
    }
    
    void OnEnable()
    {
        // 开始动画
        StartAnimation();
    }
    
    void OnDisable()
    {
        // 停止动画
        StopAnimation();
    }
    
    /// <summary>
    /// 开始播放动画
    /// </summary>
    public void StartAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        animationCoroutine = StartCoroutine(AnimateText());
    }
    
    /// <summary>
    /// 停止播放动画
    /// </summary>
    public void StopAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }
    
    /// <summary>
    /// 文字动画协程
    /// </summary>
    private IEnumerator AnimateText()
    {
        // 重置为起始状态
        textMeshPro.color = startColor;
        transform.localScale = startScale;
        
        do
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                // 计算归一化时间
                float normalizedTime = elapsedTime / duration;
                
                // 应用颜色变化
                float colorT = colorCurve.Evaluate(normalizedTime);
                textMeshPro.color = Color.Lerp(startColor, endColor, colorT);
                
                // 应用缩放变化
                float scaleT = scaleCurve.Evaluate(normalizedTime);
                transform.localScale = Vector3.Lerp(startScale, endScale, scaleT);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 确保最终值正确设置
            textMeshPro.color = endColor;
            transform.localScale = endScale;
            
            // 如果是循环模式，重置时间为0继续循环
            if (loop)
            {
                // 重置为起始状态
                textMeshPro.color = startColor;
                transform.localScale = startScale;
            }
        }
        while (loop);
        
        animationCoroutine = null;
    }
    
    /// <summary>
    /// 设置文字内容
    /// </summary>
    /// <param name="text">要显示的文字</param>
    public void SetText(string text)
    {
        if (textMeshPro != null)
        {
            textMeshPro.text = text;
        }
    }
    
    /// <summary>
    /// 设置动画持续时间
    /// </summary>
    /// <param name="newDuration">新的持续时间</param>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
        if (animationCoroutine != null)
        {
            StartAnimation(); // 重新开始动画以应用新时长
        }
    }
}