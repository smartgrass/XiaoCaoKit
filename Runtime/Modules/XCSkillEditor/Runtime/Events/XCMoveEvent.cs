using NaughtyAttributes;
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

        public AnimationCurve curve;

        public string triggerMsg;

        #region private
        private CharacterController cc;

        ///<see cref="ObjectData.otherPointName"/>
        private Matrix4x4 m4;

        private float lastTime = 0;

        public bool IsWorldTransfromMode { get; set; }

        #endregion
        public override void OnTrigger(float startOffsetTime)
        {
            m4 = Info.playerTF.localToWorldMatrix;
            cc = Info.role.idRole.cc;
            base.OnTrigger(startOffsetTime);
        }


        public override void OnUpdateEvent(int frame, float timeSinceTrigger)
        {
            float t = timeSinceTrigger / LengthTime;

            var detalMove = GetVec3Value(t) - GetVec3Value(lastTime);
            lastTime = t;
            Execute(detalMove);
        }

        public Vector3 GetVec3Value(float t)
        {
            float et = curve == null ? t : curve.Evaluate(t);

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
            //m4.MultiplyVector 等价与 Quaternion.Euler(Info.castEuler) * position;
            Vector3 move = IsWorldTransfromMode ? detalMove : m4.MultiplyVector(detalMove);

            if (task.IsMainTask && cc != null)
            {
                cc.Move(move);
            }
            else
            {
                Tran.Translate(move, Space.World);
            }
        }


        public Vector3 GetEndWoldPos()
        {
            if (!IsWorldTransfromMode)
            {
                Vector3 detalMove = endVec - startVec;
                Vector3 worldDetalMove = m4.MultiplyVector(detalMove);
                return task.GetBindTranfrom().position + worldDetalMove;
            }
            else
            {
                return endVec;
            }
        }
    }
}