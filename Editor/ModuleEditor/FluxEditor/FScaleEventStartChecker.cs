using System.Collections.Generic;
using Flux;
using UnityEngine;

namespace FluxEditor
{
    /// <summary>
    /// 缩放事件首帧值校验(只做提示,不影响任何运行时逻辑)
    /// <para>
    /// 运行时 XCScaleEvent 是在物体实际大小上做差值累加,startVec 只参与差值计算、不参与起点赋值,
    /// 所以事件首帧的实际大小必须等于 _tween.From,否则编辑器预览(绝对赋值)和运行时(相对累加)会不一致。
    /// </para>
    /// </summary>
    public static class FScaleEventStartChecker
    {
        /// <summary>逐轴允许的误差,小于该值视为一致</summary>
        public const float Epsilon = 0.001f;

        /// <summary>
        /// 取事件首帧时物体的应有大小:同轨道上前面最近的缩放事件终点;没有前置事件时取物体基础大小
        /// </summary>
        /// <param name="evt">要校验的缩放事件</param>
        /// <param name="expected">事件首帧应有的大小</param>
        /// <returns>取到参考值时返回 true</returns>
        public static bool TryGetExpectedStartScale(FTweenScaleEvent evt, out Vector3 expected)
        {
            expected = Vector3.zero;

            if (evt == null || evt.Track == null || evt.Sequence == null)
            {
                return false;
            }

            //1.前置缩放事件的终点就是本事件的起点
            bool hasPreEvent = false;
            int preEndFrame = int.MinValue;
            List<FEvent> events = evt.Track.Events;
            for (int i = 0; i < events.Count; ++i)
            {
                FTweenScaleEvent preEvt = events[i] as FTweenScaleEvent;
                if (preEvt == null || preEvt == evt || preEvt.Start >= evt.Start)
                {
                    continue;
                }

                if (preEvt.End > preEndFrame)
                {
                    preEndFrame = preEvt.End;
                    expected = preEvt.Tween.To;
                    hasPreEvent = true;
                }
            }

            if (hasPreEvent)
            {
                return true;
            }

            //2.没有前置事件:起点就是物体基础大小(保存技能数据时取的就是这个值)
            //预览播放中物体大小是临时的,不做判断,避免误报
            if (evt.Sequence.IsPlaying)
            {
                return false;
            }

            TransformSnapshot snapshot = FTransformTrack.GetSnapshot(evt.Sequence, evt.Owner);
            if (snapshot == null)
            {
                return false;
            }

            expected = snapshot.LocalScale;
            return true;
        }

        /// <summary>
        /// 事件的 From 是否与首帧应有大小对不上
        /// </summary>
        public static bool HasStartMismatch(FTweenScaleEvent evt)
        {
            if (!TryGetExpectedStartScale(evt, out Vector3 expected))
            {
                return false;
            }

            return !IsSameScale(evt.Tween.From, expected);
        }

        /// <summary>
        /// 逐轴比较两个缩放值,误差小于 Epsilon 视为一致
        /// </summary>
        public static bool IsSameScale(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) < Epsilon && Mathf.Abs(a.y - b.y) < Epsilon && Mathf.Abs(a.z - b.z) < Epsilon;
        }
    }
}
