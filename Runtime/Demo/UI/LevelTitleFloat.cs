using DG.Tweening;
using TMPro;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 关卡标题浮动动画: 从屏幕左侧滑入到中间, 停留片刻后继续向右滑出屏幕。
    /// 每段位移都用 InOutQuad 缓动, 起步加速、到点减速。
    /// 所在节点需保持全屏拉伸, 其宽度即画布宽度, 用于计算滑出屏幕的距离。
    /// </summary>
    public class LevelTitleFloat : MonoBehaviour
    {
        /// <summary>
        /// 关卡配置 levelTitleFloat 填该值表示不显示标题。
        /// </summary>
        public const string HideKeyword = "Empty";

        [Tooltip("显示标题的文本")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("入场前等待时间, 等黑屏淡出走完, 避免标题被黑屏盖住")]
        [SerializeField] private float startDelay = 0.8f;

        [Tooltip("单段滑动时长")]
        [SerializeField] private float slideDuration = 0.4f;

        [Tooltip("居中停留时长")]
        [SerializeField] private float holdDuration = 0.3f;

        private Sequence sequence;

        /// <summary>
        /// 播放一次标题动画: 屏幕左侧外部 -> 居中 -> 屏幕右侧外部, 播完自动隐藏。
        /// </summary>
        public void Play(string title)
        {
            //节点平时是隐藏的, 播完也会被隐藏
            gameObject.SetActive(true);

            sequence?.Kill();
            titleText.text = title;

            RectTransform rect = (RectTransform)transform;
            //全屏拉伸节点的宽度就是画布宽度, 再加半个文本宽度才能让标题完全移出屏幕
            float offset = rect.rect.width * 0.5f + titleText.rectTransform.rect.width * 0.5f;
            rect.anchoredPosition = new Vector2(-offset, 0f);

            sequence = DOTween.Sequence();
            sequence.AppendInterval(startDelay);
            sequence.Append(rect.DOUIMoveX(0f, slideDuration).SetEase(Ease.InOutQuad));
            sequence.AppendInterval(holdDuration);
            sequence.Append(rect.DOUIMoveX(offset, slideDuration).SetEase(Ease.InOutQuad));
            sequence.OnComplete(() => gameObject.SetActive(false));
        }

        /// <summary>
        /// 中断动画并隐藏标题。
        /// </summary>
        public void Stop()
        {
            sequence?.Kill();
            gameObject.SetActive(false);
        }
    }
}
