using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace XiaoCao
{
    //与ActiveObject的功能相似...后面再考虑扩展
    public class XCCommand_ShowAtkWarmingByMoveToPos : IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }

        public float minSwitchTime = 0.8f;

        private GameObject createObject;

        private string PrefabPath; //"Assets/_Res/SkillPrefab/Player/atk_warming_circle.prefab";

        public void Init(BaseMsg baseMsg)
        {
            PrefabPath = baseMsg.strMsg;
        }

        public void OnFinish(bool hasTrigger)
        {

            if (createObject)
            {
                createObject.gameObject.SetActive(false);
                PoolMgr.Inst.Release(PrefabPath, createObject);
            }
        }

        public void OnTrigger()
        {


            var moveEvents = curEvent.task._events.FindAll(x => x.GetType() == typeof(XCMoveEvent));

            foreach (var item in moveEvents)
            {
                //默认查找 第一最近的MoveEvent
                if (item.Start >= curEvent.Start)
                {
                    CreteWarming(item as XCMoveEvent);
                    return;
                }
            }

        }
        void CreteWarming(XCMoveEvent moveEvent)
        {
            var msg = curEvent.baseMsg;
            createObject = PoolMgr.Inst.GetOrCreatPool(PrefabPath).Get();
            createObject.transform.position = moveEvent.GetEndWoldPos();
            createObject.transform.localScale = Vector3.one * msg.numMsg;
        }



        public void OnUpdate(int frame, float timeSinceTrigger) { }

        public bool IsTargetRoleType(RoleType roleType)
        {
            return true;
        }
    }

    internal class XCCommand_MoveToTargetPos : IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }

        public bool isShoot; //保持速度,等

        public bool IsTargetRoleType(RoleType roleType)
        {
            return true;
        }

        public float minSwitchTime = 0.8f;

        public void Init(BaseMsg baseMsg)
        {
            isShoot = baseMsg.strMsg == "Shoot";
        }

        public void OnTrigger()
        {
            //获取当前Track中的MoveEvent

            var moveEvents = curEvent.task._events.FindAll(x => x.GetType() == typeof(XCMoveEvent));

            foreach (var item in moveEvents)
            {
                //找到第一最近的MoveEvent
                if (item.Start >= curEvent.Start)
                {
                    ReSetMoveEventData(item as XCMoveEvent);
                    return;
                }
            }
        }

        void ReSetMoveEventData(XCMoveEvent moveEvent)
        {
            float maxDistance = curEvent.baseMsg.numMsg;
            var role = task.Info.role;
            if (!task.Info.role.FindEnemy(out Role findRole, maxDistance + 5, angle: 90))
            {
                //如果距离过远 则放弃索敌
                return;
            }
            role.AISetLookTarget(findRole.transform);
            role.transform.RotaToPos(findRole.transform.position, 0.4f);

            //修改终点,并且修改handle
            Vector3 worldStartVec = task.GetBindTranfrom().transform.position; //获取当前世界坐标
            Vector3 worldEndVec = findRole.transform.position;

            if (isShoot)
            {
                //连线: 起点不变,终点变为过玩家方向上x距离
                Vector3 oldDelta = moveEvent.endVec - moveEvent.startVec;
                //高度差保持不变
                float endY = worldStartVec.y + oldDelta.y;
                Vector3 xzDir = (worldEndVec - worldStartVec).SetY(0).normalized;
                worldEndVec = worldStartVec + xzDir * oldDelta.magnitude + new Vector3(0, endY, 0);
            }
            else
            {
                if (Vector3.Distance(worldStartVec, worldEndVec) > maxDistance)
                {
                    worldEndVec = worldStartVec + (worldEndVec - worldStartVec).normalized * maxDistance;
                }
            }

            //修改Handle需要保持高度不变,所处距离比例不变
            if (moveEvent.isBezier)
            {
                moveEvent.handlePoint = TransformPointC(moveEvent.startVec, moveEvent.endVec, moveEvent.handlePoint,
                   worldStartVec, worldEndVec);
                //可以考虑handlePoint.y 和原来的一样
            }

            moveEvent.endVec = worldEndVec;
            moveEvent.startVec = worldStartVec;
            moveEvent.IsWorldTransfromMode = true;
        }


        /// <returns>变换后的点C'</returns>
        public static Vector3 TransformPointC(Vector3 originalA, Vector3 originalB, Vector3 originalC, Vector3 newA, Vector3 newB)
        {
            // 1. 计算平移向量
            Vector3 translation = newA - originalA;

            // 2. 计算缩放比例
            float originalLength = Vector3.Distance(originalA, originalB);
            float newLength = Vector3.Distance(newA, newB);
            float scale = newLength / originalLength;

            // 3. 计算旋转角度和轴
            Vector3 originalAB = originalB - originalA;
            Vector3 newAB = newB - newA;

            // 计算旋转轴（两向量叉乘的方向）
            Vector3 rotationAxis = Vector3.Cross(originalAB.normalized, newAB.normalized);
            if (rotationAxis.sqrMagnitude < 0.0001f)
            {
                // 如果向量平行，选择任意垂直轴
                rotationAxis = Vector3.Cross(originalAB.normalized, Vector3.up);
                if (rotationAxis.sqrMagnitude < 0.0001f)
                    rotationAxis = Vector3.Cross(originalAB.normalized, Vector3.right);
            }
            rotationAxis.Normalize();

            // 计算旋转角度
            float angle = Vector3.Angle(originalAB, newAB);

            // 4. 对C点依次应用平移、旋转和缩放变换
            // 先平移
            Vector3 translatedC = originalC + translation;

            // 再旋转（绕新的A点）
            Quaternion rotation = Quaternion.AngleAxis(angle, rotationAxis);
            Vector3 rotatedC = newA + rotation * (translatedC - newA);

            // 最后缩放（以新的A点为中心）
            Vector3 scaledC = newA + (rotatedC - newA) * scale;

            return scaledC;
        }
        public void OnFinish(bool hasTrigger) { }
        public void OnUpdate(int frame, float timeSinceTrigger) { }
    }
}
