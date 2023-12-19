using DG.Tweening;
using System;
using UnityEngine;
using XiaoCao;

namespace XiaoCao
{
    [Serializable]
    public class XCRotateEvent : XCLineEvent
    {
        private Vector3 startAngle;
        public override void OnTrigger(float timeSinceTrigger)
        {
            base.OnTrigger(timeSinceTrigger);
            startAngle = Info.castEuler + startVec;
            Tran.eulerAngles = startAngle;
        }

        public override void ApplyDetalVec(Vector3 detal)
        {
            startAngle += detal;
            Tran.eulerAngles = startAngle;
        }
    }
    [Serializable]
    public class XCScaleEvent : XCLineEvent
    {
        public override void ApplyDetalVec(Vector3 detal)
        {
            Tran.localScale += detal;
        }
    }

    [Serializable]
    //Vec线性变化事件 基类
    public class XCLineEvent : XCEvent
    {
        public Vector3 startVec;
        public Vector3 endVec;
        [NonSerialized]
        public float lastTime = 0;

        public Vector3 move => endVec - startVec;

        public Ease easeType = Ease.Linear;

        public override void OnTrigger(float timeSinceTrigger)
        {
            base.OnTrigger(timeSinceTrigger);
            lastTime = 0;

        }
        public override void OnUpdateEvent(int frame, float timeSinceTrigger)
        {
            base.OnUpdateEvent(frame, timeSinceTrigger);
            float t = timeSinceTrigger / LengthTime;

            var move = GetVec3Value(t) - GetVec3Value(lastTime);
            lastTime = t;
            ApplyDetalVec(move);
        }

        /// <summary>
        /// 执行主要逻辑
        /// </summary>
        public virtual void ApplyDetalVec(Vector3 detal) { }


        public virtual Vector3 GetVec3Value(float t)
        {
            float easingT = DOVirtual.EasedValue(0, 1, t, easeType);
            return MathTool.LinearVec3(startVec, endVec, easingT);
        }

        //public void ChageDir(float angle)
        //{
        //    //angle旋转角度 axis围绕旋转轴 position自身坐标 自身坐标 center旋转中心
        //    //Quaternion.AngleAxis(angle, axis) * (position - center) + center;

        //    startVec = MathTool.RotateY(startVec, angle);
        //    endVec = MathTool.RotateY(endVec, angle);
        //}
        //public void ChangeOffset(Vector3 offset)
        //{
        //    startVec += offset;
        //    endVec += offset;
        //}
        //public Vector3 RotateRound(Vector3 position, Vector3 center, Vector3 axis, float angle)
        //{
        //    return Quaternion.AngleAxis(angle, axis) * (position - center) + center;
        //}
    }

}
