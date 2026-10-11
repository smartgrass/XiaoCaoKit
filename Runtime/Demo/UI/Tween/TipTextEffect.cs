using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

namespace XiaoCao.UI
{
    /// <summary>
    /// 文字落地效果:主文字从落点上方淡入并缩到标准大小,落到点位那一瞬幻影文字放大淡出
    /// 参数与节奏取自 GameWinTip 的字体动画(c_GameWinTip.anim)
    /// </summary>
    [DisallowMultipleComponent]
    public class TipTextEffect : MonoBehaviour
    {
        [Label("主文字")]
        [Tooltip("从上方落下并缩到标准大小的文字,落点由它在预制体上摆好的位置决定")]
        public TMP_Text mainText;

        [Label("幻影文字")]
        [Tooltip("主文字落到点位时放大淡出的残影,位置由它在预制体上摆好的位置决定")]
        public TMP_Text ghostText;

        [Header("落地")]
        [Label("落点上方距离")]
        public float dropDistance = 120f;

        [Label("起始缩放")]
        public float startScale = 2.5f;

        [Label("落下时长")]
        public float dropDuration = 0.333f;

        [Label("主文字淡入时长")]
        public float fadeInDuration = 0.167f;

        [Label("落下缓动")]
        public Ease dropEase = Ease.InOutQuad;

        [Header("幻影")]
        [Range(0f, 1f)]
        [Label("幻影起始透明度")]
        public float ghostStartAlpha = 0.7f;

        [Label("幻影初始大小")]
        [Tooltip("幻影飘出那一刻的大小;飘出之前幻影缩为 0 隐藏,不会提前出现")]
        public float ghostStartScale;

        [Label("幻影放大到")]
        public float ghostEndScale = 1.4f;

        [Label("幻影上升高度")]
        public float ghostRiseDistance;

        [Label("幻影弹出时长")]
        public float ghostPopDuration = 0.056f;

        [Label("幻影放大时长")]
        public float ghostGrowDuration = 0.278f;

        [Label("幻影淡出时长")]
        public float ghostFadeDuration = 0.556f;

        [Header("停留与淡出")]
        [Label("停留时长")]
        public float holdDuration = 0.89f;

        [Label("淡出时长(0 = 不淡出)")]
        public float fadeOutDuration = 0.89f;

        [Label("显示时自动播放")]
        public bool playOnEnable = true;

        /// <summary>当前播放中的效果序列</summary>
        private Sequence _sequence;

        /// <summary>主文字的落点(预制体上摆好的位置),播放时从它上方落下</summary>
        private Vector2 _landPos;

        /// <summary>幻影文字的初始位置(预制体上摆好的位置),播放时从它向上飘</summary>
        private Vector2 _ghostPos;

        private void Awake()
        {
            _landPos = mainText.rectTransform.anchoredPosition;
            _ghostPos = ghostText.rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        /// <summary>
        /// 播放一次落地效果;传入 content 时会同时写进主文字与幻影文字
        /// </summary>
        public void Play(string content = null)
        {
            if (!string.IsNullOrEmpty(content))
            {
                mainText.text = content;
                ghostText.text = content;
            }

            Stop();

            RectTransform mainRect = mainText.rectTransform;
            RectTransform ghostRect = ghostText.rectTransform;

            //主文字:回到落点上方,以放大状态从透明开始
            mainText.alpha = 0f;
            mainRect.anchoredPosition = _landPos + Vector2.up * dropDistance;
            mainRect.localScale = Vector3.one * startScale;

            //幻影:飘出之前缩为 0 隐藏(不提前露出),飘出那一刻才变成初始大小
            ghostText.alpha = ghostStartAlpha;
            ghostRect.localScale = Vector3.zero;
            ghostRect.anchoredPosition = _ghostPos;

            //UI 可能处于暂停状态,效果统一走非缩放时间
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            //主文字落下:同时缩小与淡入
            _sequence.Insert(0f, AnchorPosTween(mainRect, _landPos, dropDuration).SetEase(dropEase));
            _sequence.Insert(0f, mainRect.DOScale(Vector3.one, dropDuration).SetEase(dropEase));
            _sequence.Insert(0f, AlphaTween(mainText, 1f, fadeInDuration).SetEase(Ease.Linear));

            //落点瞬间:幻影弹出、放大,并同步淡出
            //From(..., false):在补间开始时(即落地那一刻)才把大小设为幻影初始大小
            _sequence.Insert(dropDuration,
                ghostRect.DOScale(Vector3.one, ghostPopDuration).From(Vector3.one * ghostStartScale, false)
                    .SetEase(Ease.OutQuad));
            _sequence.Insert(dropDuration + ghostPopDuration,
                ghostRect.DOScale(ghostEndScale * Vector3.one, ghostGrowDuration).SetEase(Ease.Linear));
            _sequence.Insert(dropDuration, AlphaTween(ghostText, 0f, ghostFadeDuration).SetEase(Ease.Linear));

            //幻影边淡出边往上飘,飘升与淡出同步结束
            _sequence.Insert(dropDuration,
                AnchorPosTween(ghostRect, _ghostPos + Vector2.up * ghostRiseDistance, ghostFadeDuration)
                    .SetEase(Ease.Linear));

            //停留一段时间后整体淡出
            if (fadeOutDuration > 0f)
            {
                float fadeOutStart = dropDuration +
                                     Mathf.Max(ghostPopDuration + ghostGrowDuration, ghostFadeDuration) +
                                     holdDuration;
                _sequence.Insert(fadeOutStart, AlphaTween(mainText, 0f, fadeOutDuration).SetEase(Ease.Linear));
                _sequence.Insert(fadeOutStart, AlphaTween(ghostText, 0f, fadeOutDuration).SetEase(Ease.Linear));
            }
        }

        /// <summary>
        /// 停止当前效果,文字保持停止时的状态
        /// </summary>
        public void Stop()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        /// <summary>
        /// 文字透明度补间:核心 DOTween 没有 UI 模块的 Graphic.DOFade 扩展,统一走 ToAlpha
        /// </summary>
        private static Tweener AlphaTween(TMP_Text text, float endValue, float duration)
        {
            return DOTween.ToAlpha(() => text.color, value => text.color = value, endValue, duration);
        }

        /// <summary>
        /// 锚点位置补间:同上,核心 DOTween 没有 RectTransform.DOAnchorPos 扩展
        /// </summary>
        private static Tweener AnchorPosTween(RectTransform rect, Vector2 endValue, float duration)
        {
            return DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, endValue, duration);
        }
    }
}
