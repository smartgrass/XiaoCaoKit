using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 节点缩放补间:显示时(OnEnable)播放当前节点自身的 localScale 动画, 从起始缩放过渡到目标缩放。
    /// 支持单次、循环、往返三种播放模式, Inspector 上可点 play / stop 按钮直接试效果。
    /// </summary>
    [DisallowMultipleComponent]
    public class ScaleTween : MonoBehaviour
    {
        /// <summary>播放模式</summary>
        public enum LoopMode
        {
            /// <summary>只播放一次,结束后停在目标缩放</summary>
            Once,

            /// <summary>循环播放,每轮回到起始缩放重新开始</summary>
            Loop,

            /// <summary>往返播放,在起始与目标缩放之间来回</summary>
            PingPong,
        }

        [Label("起始缩放")]
        [Tooltip("每次播放开始时先写回这个缩放")]
        public Vector3 startScale = Vector3.one * 0.8f;

        [Label("目标缩放")]
        public Vector3 endScale = Vector3.one;

        [Label("启始等待")]
        [Tooltip("播放前的等待时间(秒), 等待期间保持起始缩放不动")]
        public float startDelay = 0f;

        [Label("时长")]
        [Tooltip("单程动画时长(秒)")]
        public float duration = 0.25f;

        [Label("缓动")]
        public Ease ease = Ease.OutBack;

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
        /// 从起始缩放开始播放;重复调用会先停掉当前补间再重新开始
        /// </summary>
        [Button("play")]
        public void Play()
        {
            Stop();

            transform.localScale = startScale;

            //SetLink:节点销毁时自动回收补间,避免补间残留在 DOTween 里
            _tweener = transform.DOScale(endScale, duration)
                .SetEase(ease)
                .SetLink(gameObject);

            //启始等待:延迟期间补间还没开始插值, localScale 就停在起始缩放不动
            if (startDelay > 0f)
            {
                _tweener.SetDelay(startDelay);
            }

            switch (loopMode)
            {
                //循环用的是 DOTween 的 LoopType,写全限定名避免和 UITween.LoopType 混淆
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
        /// 停止播放,缩放保持停止时的状态
        /// </summary>
        [Button("stop")]
        public void Stop()
        {
            _tweener?.Kill();
            _tweener = null;
        }
    }
}
