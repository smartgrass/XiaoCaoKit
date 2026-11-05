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

        /// <summary>前方朝向运动方向(速度方向), 抛物线子弹/投掷物用</summary>
        public bool faceVelocity;

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
            //子技能用生成帧缓存的玩家矩阵, 生成后玩家翻滚/转向不再改变运动方向;
            //主任务没有 ObjectData(角色自身位移), 仍取实时矩阵
            m4 = task.ObjectData != null ? task.ObjectData.SpawnMatrix : Info.playerTF.localToWorldMatrix;
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
                if (faceVelocity)
                {
                    FaceMoveDirection(move);
                }
            }
        }

        /// <summary>
        /// 让物体前方对齐本帧位移方向(速度方向), 弹道类的子弹用它保持"弹头朝前"
        /// </summary>
        private void FaceMoveDirection(Vector3 move)
        {
            if (move.sqrMagnitude < 1e-8f)
            {
                return;
            }

            Vector3 dir = move.normalized;
            //接近垂直升降时 LookRotation 的 up 会退化, 换一个参考上方向
            Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.999f ? Vector3.forward : Vector3.up;
            Tran.rotation = Quaternion.LookRotation(dir, up);
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