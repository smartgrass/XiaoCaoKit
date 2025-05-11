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

        //特殊处理
        public ETriggerCmd command;
        public string triggerMsg;

        #region private
        private CharacterController cc;

        private Matrix4x4 m4;

        private float lastTime = 0;

        #endregion
        public override void OnTrigger(float startOffsetTime)
        {
            m4 = Info.playerTF.localToWorldMatrix;
            cc = Info.role.idRole.cc;
            if (command!= ETriggerCmd.None)
            {
                //利用中间类执行,解耦
                //TriggerCommondHelper.AddCommond(command, triggerMsg, this);
            }
            base.OnTrigger(startOffsetTime);
        }

        public void ResetStartEnd(Vector3 startVec,Vector3 endVec)
        {
            //空判断
            Debug.Log($"--- ResetStartEnd");
            this.startVec = startVec;
            this.endVec = endVec;
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
            float et = curve == null ? t: curve.Evaluate(t);

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
            if (cc != null && task.IsMainTask)
            {
                // 等价与 Quaternion.Euler(Info.castEuler) * position;
                cc.Move(m4.MultiplyVector(detalMove));
            }
            else
            {
                var getDelta = m4.MultiplyVector(detalMove);
                Tran.Translate(getDelta, Space.World);
                //Tran.position = Tran.position + getDelta;
            }

            //if (lookForward) //TODO
            //{
            //    Tf.forward = m4.MultiplyVector(detalMove);
            //}

        }

    }

}