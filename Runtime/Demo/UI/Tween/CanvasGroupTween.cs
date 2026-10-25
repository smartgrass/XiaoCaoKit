using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// CanvasGroup 透明度补间:把整组 UI 的 alpha 从起始值过渡到目标值(面板淡入淡出用)。
    /// 支持单次、循环、往返三种播放模式,Inspector 上可点 play / stop 按钮直接试效果。
    /// </summary>
    [DisallowMultipleComponent]
    public class CanvasGroupTween : MonoBehaviour
    {
        /// <summary>播放模式</summary>
        public enum LoopMode
        {
            /// <summary>只播放一次,结束后停在目标透明度</summary>
            Once,

            /// <summary>循环播放,每轮回到起始透明度重新开始</summary>
            Loop,

            /// <summary>往返播放,在起始与目标透明度之间来回</summary>
            PingPong,
        }

        [Label("目标组")]
        [Tooltip("要改变透明度的 CanvasGroup,通常就是这个节点自己身上的")]
        public CanvasGroup canvasGroup;

        [Label("起始透明度")]
        [Range(0f, 1f)]
        [Tooltip("每次播放开始时先写回这个值")]
        public float startAlpha;

        [Label("目标透明度")]
        [Range(0f, 1f)]
        public float endAlpha = 1f;

        [Label("时长")]
        [Tooltip("单程动画时长(秒)")]
        public float duration = 0.3f;

        [Label("缓动")]
        public Ease ease = Ease.OutQuad;

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
        /// 从起始透明度开始播放;重复调用会先停掉当前补间再重新开始
        /// </summary>
        [Button("play")]
        public void Play()
        {
            Stop();

            canvasGroup.alpha = startAlpha;

            //核心 DOTween 没有 CanvasGroup.DOFade(UI 模块被 #if false 关掉了),统一走 DOTween.To
            //SetLink:组件销毁时自动回收补间,避免补间残留在 DOTween 里继续访问已销毁的 CanvasGroup
            _tweener = DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, endAlpha, duration)
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
        /// 停止播放,透明度保持停止时的状态
        /// </summary>
        [Button("stop")]
        public void Stop()
        {
            _tweener?.Kill();
            _tweener = null;
        }
    }
}
