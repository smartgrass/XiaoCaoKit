using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    /// <summary>
    /// 通用 Graphic 动画组件:TMP 文字、Image、RawImage 都能用。
    /// 按配置的曲线在起始/结束状态之间插值, 同时改变颜色与缩放, 支持单次、循环、往返三种播放方式。
    /// </summary>
    public class UITween : MonoBehaviour
    {
        /// <summary>播放方式</summary>
        public enum LoopType
        {
            /// <summary>每轮从起始状态重新开始</summary>
            Repeat,

            /// <summary>在起始与结束状态之间往返</summary>
            PingPong
        }

        [Header("动画设置")]
        [Tooltip("单程动画持续时间")]
        public float duration = 2f;

        [Tooltip("是否循环播放; 不勾选则只播一遍")]
        public bool loop = true;

        [Tooltip("循环时的往返方式")]
        public LoopType loopType = LoopType.Repeat;

        [Tooltip("每轮之间的等待时间")]
        public float loopWaitTime = 0f;

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

        [Header("引用")]
        [Tooltip("要播放动画的 Graphic(TMP 文字 / Image / RawImage), 在 Inspector 上绑定")]
        public Graphic graphic;

        /// <summary>当前播放中的协程</summary>
        private Coroutine animationCoroutine;

        private void OnEnable()
        {
            StartAnimation();
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        /// <summary>
        /// 开始播放动画; 重复调用会先停掉上一轮再从头开始
        /// </summary>
        public void StartAnimation()
        {
            //节点未激活时 StartCoroutine 会抛 "game object xxx is inactive",
            //这里直接跳过, 激活后 OnEnable 会自己再播一次
            if (!isActiveAndEnabled)
            {
                animationCoroutine = null;
                return;
            }

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(AnimateGraphic());
        }

        /// <summary>
        /// 停止播放动画, 颜色与缩放保持停止时的状态
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
        /// 颜色 + 缩放动画协程: 按曲线插值, 每轮结束按 loop / loopType 决定是否继续
        /// </summary>
        private IEnumerator AnimateGraphic()
        {
            //没绑 Graphic 时直接结束, 不然每帧空引用
            if (graphic == null)
            {
                animationCoroutine = null;
                yield break;
            }

            //duration 为 0 时直接跳到终点, 这里保底避免除零
            float totalTime = Mathf.Max(duration, 0.01f);

            //重置为起始状态
            graphic.color = startColor;
            transform.localScale = startScale;

            bool isForward = true;

            while (true)
            {
                float elapsedTime = 0f;

                while (elapsedTime < totalTime)
                {
                    float normalizedTime = elapsedTime / totalTime;

                    //往返回程时把时间反过来取
                    if (!isForward)
                    {
                        normalizedTime = 1f - normalizedTime;
                    }

                    graphic.color = Color.Lerp(startColor, endColor, colorCurve.Evaluate(normalizedTime));
                    transform.localScale = Vector3.Lerp(startScale, endScale, scaleCurve.Evaluate(normalizedTime));

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                //补上终点帧, 避免低帧率下停在半路
                if (isForward)
                {
                    graphic.color = endColor;
                    transform.localScale = endScale;
                }
                else
                {
                    graphic.color = startColor;
                    transform.localScale = startScale;
                }

                if (loopWaitTime > 0f)
                {
                    yield return new WaitForSeconds(loopWaitTime);
                }

                if (!loop)
                {
                    break;
                }

                if (loopType == LoopType.PingPong)
                {
                    isForward = !isForward;
                }
                else
                {
                    graphic.color = startColor;
                    transform.localScale = startScale;
                    isForward = true;
                }
            }

            animationCoroutine = null;
        }

        /// <summary>
        /// 设置动画时长; 正在播放时会立即用新时长重播
        /// </summary>
        public void SetDuration(float newDuration)
        {
            duration = newDuration;

            if (animationCoroutine != null)
            {
                StartAnimation();
            }
        }
    }
}
