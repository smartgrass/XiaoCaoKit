using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    /// <summary>
    /// 通用颜色补间:把 Graphic(Image / TextMeshPro / RawImage 等)的颜色从起始色过渡到目标色。
    /// 支持单次、循环、往返三种播放模式,Inspector 上可点 play / stop 按钮直接试效果。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIColorTween : MonoBehaviour
    {
        /// <summary>播放模式</summary>
        public enum LoopMode
        {
            /// <summary>只播放一次,结束后停在目标颜色</summary>
            Once,

            /// <summary>循环播放,每轮回到起始颜色重新开始</summary>
            Loop,

            /// <summary>往返播放,在起始颜色与目标颜色之间来回</summary>
            PingPong,
        }

        [Label("目标图形")]
        [Tooltip("要改变颜色的组件,Image、TextMeshPro、RawImage 都可以直接拖进来")]
        public Graphic graphic;

        [Label("起始颜色")]
        [Tooltip("每次播放开始时先写回这个颜色")]
        public Color startColor = Color.white;

        [Label("目标颜色")]
        public Color endColor = Color.red;

        [Label("时长")]
        [Tooltip("单程动画时长(秒)")]
        public float duration = 1f;

        [Label("缓动")]
        public Ease ease = Ease.InOutQuad;

        [Label("播放模式")]
        public LoopMode loopMode = LoopMode.Once;

        [Label("显示时自动播放")]
        public bool playOnEnable = true;

        /// <summary>当前播放中的补间</summary>
        private Tweener _tweener;

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
        /// 从起始颜色开始播放;重复调用会先停掉当前补间再重新开始
        /// </summary>
        [Button("play")]
        public void Play()
        {
            Stop();

            graphic.color = startColor;

            //核心 DOTween 没有 UI 模块的 Graphic.DOColor,统一走 DOTween.To
            //SetLink:组件销毁时自动回收补间,避免补间残留在 DOTween 里访问已销毁的 Graphic
            _tweener = DOTween.To(() => graphic.color, value => graphic.color = value, endColor, duration)
                .SetEase(ease)
                .SetLink(gameObject);

            switch (loopMode)
            {
                //循环/往返用的是 DOTween 的 LoopType,写全限定名避免和 UITween.LoopType 混淆
                case LoopMode.Loop:
                    _tweener.SetLoops(-1, DG.Tweening.LoopType.Restart);
                    break;
                case LoopMode.PingPong:
                    _tweener.SetLoops(-1, DG.Tweening.LoopType.Yoyo);
                    break;
                default:
                    _tweener.SetLoops(1);
                    break;
            }
        }

        /// <summary>
        /// 停止播放,颜色保持停止时的状态
        /// </summary>
        [Button("stop")]
        public void Stop()
        {
            _tweener?.Kill();
            _tweener = null;
        }
    }
}
