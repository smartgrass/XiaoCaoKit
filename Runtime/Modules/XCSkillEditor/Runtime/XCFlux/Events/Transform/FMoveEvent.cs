using DG.Tweening;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using XiaoCao;

namespace Flux
{
    //FMoveEventEditor
    [FEvent("Transform/Tween Move", typeof(FMoveTrack))]
    public class FMoveEvent : FEvent
    {
        //n
        public List<Vector3> controlPoints = new List<Vector3>(); // 存储贝塞尔曲线上的控制点
        //n-1
        public List<Vector3> handlePionts = new List<Vector3>();
        //n-2 头尾0,1省略
        public List<float> arrivalTimes = new List<float>(); // 存储每个点到达的时间


        public List<AnimationCurve> curves = new List<AnimationCurve>();

        //水平方向上旋转
        public float horAngle = 0;

        //List<Ease>
        [SerializeField]
        private bool _isBezier;
        public bool isEditorHandles;

        //public ETriggerCmd command;
        //[Dropdown(nameof(selectList2))]
        //public string triggerMsg;

        //private string[] selectList2 = new[] { 
        //    "0",
        //    TriggerCommond_XCMove.WeaponFirePointName
        //};


        public bool IsBezier
        {
            get => _isBezier; set
            {
                _isBezier = value;
                OnBezierChange();
            }
        }

        protected override void OnInit()
        {
            base.OnInit();
            CheckLen();
        }

        public void CheckLen()
        {
            if (controlPoints.Count < 2)
            {
                controlPoints.Add(Vector3.zero);
                controlPoints.Add(Vector3.forward);
            }
            int len = controlPoints.Count;

            if (IsBezier)
            {
                int n1 = len - 1;
                if (n1 > handlePionts.Count)
                {
                    int deltaLen = n1 - handlePionts.Count;
                    AddhandlePonts(deltaLen);
                }
            }

            int n2 = len - 2;
            if (n2 != arrivalTimes.Count)
            {
                int deltalen = n2 - arrivalTimes.Count;
                AddarrivalTimes(deltalen);
            }

        }

        private void AddarrivalTimes(int deltalen)
        {
            if (deltalen < 0)
            {
                for (int i = 0; i < -deltalen; i++)
                {
                    arrivalTimes.RemoveAt(arrivalTimes.Count - 1);
                }
                return;
            }

            int len = arrivalTimes.Count;
            //获取最后一段长度
            float lastDelta = 1f;
            if (len == 0)
            {
                lastDelta = 1;
            }
            else
            {
                lastDelta = 1f - arrivalTimes[len - 1];
            }
            //新节点追加进总长
            float total = 1 + deltalen * lastDelta;

            for (int i = 0; i < len + deltalen; i++)
            {
                if (i < len)
                {
                    arrivalTimes[i] = arrivalTimes[i] / total;
                }
                else
                {
                    int addIndex = i - len;
                    arrivalTimes.Add((1 + addIndex * lastDelta) / total);
                }
            }
            SortTimes();
        }

        //本来+ controlPoints 用的
        private void AddhandlePonts(int deltaLen)
        {
            int startCount = handlePionts.Count;
            int len = controlPoints.Count;

            for (int i = 0; i < deltaLen; i++)
            {
                int startIndex = startCount + i;
                var deltaVec = controlPoints[startIndex + 1] - controlPoints[startIndex];
                Vector3 C = controlPoints.Count > startIndex + 2 ? controlPoints[startIndex + 2] :
                    controlPoints[startIndex + 1] + deltaVec;

                Vector3 nextPonit = (controlPoints[startIndex] + controlPoints[startIndex + 1]) / 2;
                handlePionts.Add(nextPonit);
            }
        }

        private void AddControlPoints(int deltaLen)
        {
            int len = controlPoints.Count;
            var deltaVec = controlPoints[len - 1] - controlPoints[len - 2];
            for (int i = 0; i < deltaLen; i++)
            {

                controlPoints.Add(controlPoints[len - 1] + deltaVec * (i + 1));

            }
        }

        public void SortTimes()
        {
            arrivalTimes.Sort(new FloatSort());
        }
        class FloatSort : IComparer<float>
        {
            public int Compare(float f1, float f2)
            {
                if (f1 > f2)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        protected override void OnTrigger(float timeSinceTrigger)
        {
            OnUpdateEvent(timeSinceTrigger);
        }

        protected override void OnUpdateEvent(float timeSinceTrigger)
        {
            float t = timeSinceTrigger / LengthTime;

            ApplyProperty(t);
        }

        protected override void OnFinish()
        {
            ApplyProperty(1f);
        }

        protected override void OnStop()
        {
            ApplyProperty(0f);
        }

        void ApplyProperty(float t)
        {
            if (controlPoints.Count < 2)
            {

                return;
            }

            Vector3 currentPosition = GetBezierPathPositionAndIndex(t, out int nextIndex);
            Owner.transform.position = currentPosition + Owner.transform.parent.position;
        }

        public Vector3 GetBezierPathPositionAndIndex(float normalizedTime, out int timeIndex)
        {
            int timeCount = arrivalTimes.Count;
            timeIndex = 0;
            // 寻找当前时间所在的区间
            for (int i = 0; i < timeCount; i++)
            {
                if (arrivalTimes[i] >= normalizedTime)
                {
                    break;
                }
                timeIndex++;
            }

            // 计算插值百分比
            float tBetweenPoints = 0.0f;
            float curTimeLen = 0.0f;
            float curTime = 0;

            if (timeIndex == 0)
            {
                curTimeLen = GetTime(0);
                curTime = normalizedTime;
            }
            else
            {
                float lastTime = GetTime(timeIndex - 1);
                curTimeLen = GetTime(timeIndex) - lastTime;
                if (curTimeLen == 0)
                {
                    return controlPoints[timeIndex + 1];
                }

                curTime = normalizedTime - lastTime;
            }
            tBetweenPoints = curTime / curTimeLen;

            //Debug.Log($" {normalizedTime} {timeIndex} {tBetweenPoints}  {curTime} / {curTimeLen}");

            //曲线缩放
            tBetweenPoints = Evaluate(timeIndex, tBetweenPoints);

            if (IsBezier)
            {
                return MathTool.GetBezierPoint2(controlPoints[timeIndex], controlPoints[timeIndex + 1], handlePionts[timeIndex], tBetweenPoints);
            }
            else
            {
                Vector3 start = controlPoints[timeIndex];
                Vector3 end = controlPoints[timeIndex + 1];

                start = MathTool.RotateY(start, horAngle);
                end = MathTool.RotateY(end, horAngle);  
                return MathTool.LinearVec3(start, end, tBetweenPoints);
            }
        }

        public void OnBezierChange()
        {
            if (IsBezier)
            {
                CheckLen();
            }
        }
        public void OnNumChange()
        {
            CheckLen();
        }

        public float Evaluate(int i, float t)
        {
            if (curves.Count > i)
            {
                return curves[i].Evaluate(t);
            }
            return t;
        }

        public AnimationCurve GetEase(int i)
        {
            if (curves.Count > i)
            {
                return curves[i];
            }
            return null;
        }
        //对于controlpoint,
        public float GetTime(int index)
        {
            if (arrivalTimes.Count == index)
            {
                return 1;
            }
            else if (index < 0)
            {
                return 0;
            }
            return arrivalTimes[index];
        }

        public List<XCMoveEvent> ToXCEventList()
        {
            FMoveEvent fe = this;
            List<XCMoveEvent> list = new List<XCMoveEvent>();
            int len = fe.controlPoints.Count;
            for (int i = 0; i < len - 1; i++)
            {
                XCMoveEvent xcMove = new XCMoveEvent();
                var start = fe.controlPoints[i];
                var end = fe.controlPoints[i + 1];
                //补充
                start = MathTool.RotateY(start, fe.horAngle);
                end = MathTool.RotateY(end, fe.horAngle);

                xcMove.isBezier = fe.IsBezier;
                if (fe.IsBezier)
                {
                    xcMove.handlePoint = fe.handlePionts[i];
                }

                xcMove.startVec = start;
                xcMove.endVec = end;
                xcMove.curve = fe.GetEase(i);

                float startTime = fe.GetTime(i - 1);
                float endTime = fe.GetTime(i);

                int startFrame = (int)Mathf.Lerp(fe.Start, fe.End, startTime);
                int endFrame = (int)Mathf.Lerp(fe.Start, fe.End, endTime);
                xcMove.range = new XCRange(startFrame, endFrame);
                list.Add(xcMove);
            }
            return list;
        }

    }

    public static class FEaseExtend
    {
        public static Ease FEaseToEase(this FEase fEase)
        {
            Ease ease = (Ease)Enum.Parse(typeof(Ease), fEase.ToString());
            return ease;
        }
    }


    public enum FEase
    {
        [InspectorName("Linear")]
        Linear,
        [InspectorName("加速")]
        InQuad,
        [InspectorName("减速")]
        OutQuad,
        [InspectorName("淡入淡出")]
        InOutQuad,
        [InspectorName("加速2")]
        InCubic,
        [InspectorName("减速2")]
        OutCubic,
        InOutCubic,
        [InspectorName("加速3")]
        InQuart,
        [InspectorName("减速3")]
        OutQuart,
        InOutQuart,
        [InspectorName("加速4")]
        InQuint,
        [InspectorName("减速4")]
        OutQuint,
        InOutQuint,
        [InspectorName("加速5-1")]
        InExpo,
        [InspectorName("减速5-1")]
        OutExpo,
        InOutExpo,
        [InspectorName("加速5-2")]
        InCirc,
        [InspectorName("减速5-2")]
        OutCirc,
        InOutCirc,
    }

}
