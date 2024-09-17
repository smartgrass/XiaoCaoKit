using DG.Tweening;
using NaughtyAttributes;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 以位移差作为移动
    /// </summary>
    public class XCMoveEvent : XCEvent
    {
        public bool isBezier;

        public Vector3 startVec;

        public Vector3 endVec;

        [ShowIf(nameof(isBezier))]
        public Vector3 handlePoint;

        public Ease easeType = Ease.Linear;

        #region private
        private CharacterController cc;

        private Matrix4x4 m4;

        private float lastTime = 0;

        #endregion
        public override void OnTrigger(float startOffsetTime)
        {
            Debug.Log($"--- move OnTrigger");
            m4 = Tran.localToWorldMatrix;
            cc = Info.role.idRole.cc;
            base.OnTrigger(startOffsetTime);
        }

        public override void OnUpdateEvent(int frame, float timeSinceTrigger)
        {
            DebugGUI.Log($"move frame",frame,task._events.Count);
            float t = timeSinceTrigger / LengthTime;


            var detalMove = GetVec3Value(t) - GetVec3Value(lastTime);
            lastTime = t;
            Execute(detalMove);
        }

        public Vector3 GetVec3Value(float t)
        {
            float et = DOVirtual.EasedValue(0, 1, t, easeType);

            if (isBezier)
            {
                return MathTool.GetBezierPoint2(startVec, endVec, handlePoint, et);
            }
            else
            {
                return MathTool.LinearVec3(startVec, endVec, et);
            }
        }

        public void Execute(Vector3 detalMove)
        {
            if (detalMove.IsZore())
            {
                return;
            }
            if (cc != null)
            {
                // 等价与 Quaternion.Euler(Info.castEuler) * position;
                cc.Move(m4.MultiplyVector(detalMove));
            }
            else
            {
                var getDelta = m4.MultiplyVector(detalMove);
                Tran.Translate(getDelta, Space.World);
            }

            //if (lookForward) //TODO
            //{
            //    Tf.forward = m4.MultiplyVector(detalMove);
            //}

        }

    }

}