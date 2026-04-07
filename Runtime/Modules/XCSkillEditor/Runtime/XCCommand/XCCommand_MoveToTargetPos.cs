using System;
using UnityEngine;

namespace XiaoCao
{
    internal class XCCommand_MoveToTargetPos : BaseCommand
    {
        private const string DefaultAtkWarmingPrefabPath = "Assets/_Res/SkillPrefab/Player/atk_warming_circle.prefab";

        public bool isShoot; //保持速度,等
        public bool isDropDown;

        public float offset;

        public float minDistance;

        private GameObject createObject;

        private bool showAtkWarming;
        private float atkWarmingScale;
        private string atkWarmingPrefabPath;

        public override void Init(BaseMsg baseMsg)
        {
            IsOtherMsg = true;
            ResetOtherMsgData();
            createObject = null;
            isShoot = false;
            isDropDown = false;
            offset = 0;
            minDistance = 0;
            string[] array = string.IsNullOrWhiteSpace(baseMsg.strMsg) ? Array.Empty<string>() : baseMsg.strMsg.Split(",");
            if (array.Length > 0)
            {
                //如offset_-0.5,min_1
                foreach (var str in array)
                {
                    string token = str.Trim();
                    if (token.Equals("Shoot", StringComparison.OrdinalIgnoreCase))
                    {
                        isShoot = true;
                    }
                    else if (token.Equals("dropDown", StringComparison.OrdinalIgnoreCase))
                    {
                        isDropDown = true;
                    }
                    else if (token.StartsWith("offset"))
                    {
                        string numStr = token.Split("_")[1];
                        offset = float.Parse(numStr);
                    }
                    else if (token.StartsWith("min"))
                    {
                        string numStr = token.Split("_")[1];
                        minDistance = float.Parse(numStr);
                    }
                }
            }
        }

        public override void InitOtherMsg(string[] otherMsgs)
        {
            ResetOtherMsgData();
            if (otherMsgs == null || otherMsgs.Length == 0)
            {
                return;
            }

            foreach (string otherMsg in otherMsgs)
            {
                ParseOtherMsg(otherMsg);
            }
        }

        public override void OnTrigger()
        {
            //获取当前Track中的MoveEvent
            XCMoveEvent moveEvent = GetMoveEvent();
            if (moveEvent == null)
            {
                return;
            }

            ReSetMoveEventData(moveEvent);
            CreateAtkWarming(moveEvent);
        }

        private XCMoveEvent GetMoveEvent()
        {
            var moveEvents = curEvent.task._events.FindAll(x => x.GetType() == typeof(XCMoveEvent));

            foreach (var item in moveEvents)
            {
                //找到第一最近的MoveEvent
                if (item.Start >= curEvent.Start)
                {
                    return item as XCMoveEvent;
                }
            }

            return null;
        }

        void ReSetMoveEventData(XCMoveEvent moveEvent)
        {
            float maxDistance = curEvent.baseMsg.numMsg;
            float searchDistance = maxDistance + 5;
            var role = task.Info.role;
            if (maxDistance == 0)
            {
                maxDistance = 5;
            }

            if (isShoot)
            {
                moveEvent.task.ObjectData.ReSetStartPos();
            }

            //修改终点,并且修改handle
            Vector3 worldStartVec = task.GetBindTranfrom().position; //获取当前世界坐标
            bool hasEnemy = task.Info.role.FindEnemy(out Role findRole, searchDistance, angle: 50);
            Vector3 worldEndVec = hasEnemy ? findRole.transform.position : GetVirtualFocusPos(worldStartVec, role.transform, maxDistance);

            if (hasEnemy)
            {
                role.AISetLookTarget(findRole.transform);
            }
            else
            {
                role.AISetLookTarget(null);
            }
            role.transform.RotaToPos(worldEndVec, 0.4f);

            if (offset != 0)
            {
                worldEndVec += (worldEndVec - worldStartVec).normalized * offset;
            }

            if (minDistance != 0)
            {
                if (Vector3.Distance(worldStartVec, worldEndVec) < minDistance)
                {
                    worldEndVec = worldStartVec + (worldEndVec - worldStartVec).normalized * minDistance;
                }
            }

            if (isShoot)
            {
                //连线: 起点不变,终点变为过玩家方向上x距离
                Vector3 oldDelta = moveEvent.endVec - moveEvent.startVec;
                //高度差保持不变
                Vector3 xzDir = (worldEndVec - worldStartVec).SetY(0).normalized;
                worldEndVec = worldStartVec + xzDir * oldDelta.magnitude + new Vector3(0, oldDelta.y, 0);
            }
            else
            {
                if (Vector3.Distance(worldStartVec, worldEndVec) > maxDistance)
                {
                    worldEndVec = worldStartVec + (worldEndVec - worldStartVec).normalized * maxDistance;
                }
            }

            if (isDropDown)
            {
                worldEndVec = AlignToGround(worldEndVec);
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

        private Vector3 GetVirtualFocusPos(Vector3 worldStartVec, Transform roleTransform, float maxDistance)
        {
            Vector3 forward = roleTransform.forward.SetY(0);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return worldStartVec + forward.normalized * maxDistance;
        }

        private Vector3 AlignToGround(Vector3 targetPos)
        {
            Vector3 rayStart = targetPos + Vector3.up * 20f;
            float rayDistance = 200f;
            int layerMask = Layers.GROUND_MASK | Layers.DEFAULT_MASK;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                targetPos.y = hit.point.y;
            }

            return targetPos;
        }

        /// <summary>
        /// 将原始三角形中的 C 点，映射到新的 AB 线段上。
        /// 这里不直接用“把原 AB 旋转到新 AB”的方式，因为那样在 AB 方向相同但绕 AB 轴存在自由旋转时，
        /// 会让 C 点所在平面发生额外扭转，例如原来与地面垂直的平面，重算后可能不再垂直。
        /// 当前实现会先为原始 AB 和目标 AB 各自构建一个稳定坐标系，再把 C-A 的局部坐标映射过去。
        /// </summary>
        /// <returns>变换后的点C'</returns>
        public static Vector3 TransformPointC(Vector3 originalA, Vector3 originalB, Vector3 originalC, Vector3 newA,
            Vector3 newB)
        {
            float originalLength = Vector3.Distance(originalA, originalB);
            if (originalLength < 0.0001f)
            {
                // 原始 AB 太短时无法建立稳定方向，直接回退到新起点。
                return newA;
            }

            float newLength = Vector3.Distance(newA, newB);
            if (newLength < 0.0001f)
            {
                // 目标 AB 太短时同样无法完成映射。
                return newA;
            }

            float scale = newLength / originalLength;

            Vector3 originalAB = originalB - originalA;
            Vector3 newAB = newB - newA;
            Vector3 originalAC = originalC - originalA;
            // 原始三点平面的法线，用来帮助构造更稳定的局部坐标系。
            Vector3 planeNormalHint = Vector3.Cross(originalAB, originalAC);

            BuildStableBasis(originalAB, planeNormalHint, out Vector3 originalForward, out Vector3 originalUp,
                out Vector3 originalRight);
            BuildStableBasis(newAB, planeNormalHint, out Vector3 newForward, out Vector3 newUp, out Vector3 newRight);

            // 先把 C 相对 A 的位置投影到“原始稳定坐标系”中，再按比例缩放。
            Vector3 localOffset = new Vector3(
                Vector3.Dot(originalAC, originalForward),
                Vector3.Dot(originalAC, originalUp),
                Vector3.Dot(originalAC, originalRight)) * scale;

            // 最后再把这个局部偏移写回“目标稳定坐标系”。
            return newA
                + newForward * localOffset.x
                + newUp * localOffset.y
                + newRight * localOffset.z;
        }

        /// <summary>
        /// 根据给定方向构建一个稳定的正交基。
        /// 会优先参考世界 Up，这样当原始平面本来与地面存在稳定关系时，重算后更容易保持一致。
        /// </summary>
        private static void BuildStableBasis(Vector3 direction, Vector3 planeNormalHint, out Vector3 forward,
            out Vector3 up, out Vector3 right)
        {
            forward = direction.normalized;

            // 优先使用世界 Up 来固定“绕 forward 的旋转自由度”。
            right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude < 0.0001f && planeNormalHint.sqrMagnitude > 0.0001f)
            {
                // 当 forward 与世界 Up 近乎平行时，再退回到原始平面法线辅助定向。
                right = Vector3.Cross(planeNormalHint, forward);
            }

            if (right.sqrMagnitude < 0.0001f)
            {
                // 极端情况下继续使用固定轴兜底，保证一定能构造出坐标系。
                right = Vector3.Cross(Vector3.forward, forward);
            }

            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.Cross(Vector3.right, forward);
            }

            right.Normalize();
            up = Vector3.Cross(forward, right).normalized;
        }

        public override void OnFinish(bool hasTrigger)
        {
            if (createObject)
            {
                createObject.gameObject.SetActive(false);
                PoolMgr.Inst.Release(GetAtkWarmingPrefabPath(), createObject);
                createObject = null;
            }
        }

        public override void OnUpdate(int frame, float timeSinceTrigger)
        {
        }

        private void CreateAtkWarming(XCMoveEvent moveEvent)
        {
            if (!showAtkWarming)
            {
                return;
            }

            createObject = PoolMgr.Inst.GetOrCreatPool(GetAtkWarmingPrefabPath()).Get();
            createObject.transform.position = moveEvent.GetEndWoldPos();
            createObject.transform.localScale = Vector3.one * atkWarmingScale;
        }

        private string GetAtkWarmingPrefabPath()
        {
            return string.IsNullOrEmpty(atkWarmingPrefabPath) ? DefaultAtkWarmingPrefabPath : atkWarmingPrefabPath;
        }

        private void ResetOtherMsgData()
        {
            showAtkWarming = false;
            atkWarmingScale = 1f;
            atkWarmingPrefabPath = DefaultAtkWarmingPrefabPath;
        }

        private void ParseOtherMsg(string otherMsg)
        {
            if (string.IsNullOrWhiteSpace(otherMsg))
            {
                return;
            }

            string[] tokens = otherMsg.Split(',');
            if (TryParseAtkWarmingConfig(tokens))
            {
                return;
            }

            foreach (string rawToken in tokens)
            {
                string token = NormalizeOtherMsgToken(rawToken);
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (float.TryParse(token, out float directScale) && directScale > 0)
                {
                    showAtkWarming = true;
                    atkWarmingScale = directScale;
                }
                if (token.Equals("AtkWarming", StringComparison.OrdinalIgnoreCase)
                    || token.Equals("ShowAtkWarming", StringComparison.OrdinalIgnoreCase))
                {
                    showAtkWarming = true;
                }
                else if (token.StartsWith("show_", StringComparison.OrdinalIgnoreCase))
                {
                    string showStr = token.Substring("show_".Length);
                    if (bool.TryParse(showStr, out bool show))
                    {
                        showAtkWarming = show;
                    }
                }
                else if (token.StartsWith("path_", StringComparison.OrdinalIgnoreCase))
                {
                    atkWarmingPrefabPath = token.Substring("path_".Length);
                }
                else if (token.StartsWith("scale_", StringComparison.OrdinalIgnoreCase))
                {
                    string scaleStr = token.Substring("scale_".Length);
                    if (float.TryParse(scaleStr, out float scale) && scale > 0)
                    {
                        atkWarmingScale = scale;
                    }
                }
                else if (token.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    atkWarmingPrefabPath = token;
                }
            }
        }

        private bool TryParseAtkWarmingConfig(string[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                return false;
            }

            string typeToken = NormalizeOtherMsgToken(tokens[0]);
            if (!typeToken.Equals("AtkWarming", StringComparison.OrdinalIgnoreCase)
                && !typeToken.Equals("ShowAtkWarming", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            showAtkWarming = true;

            if (tokens.Length > 1)
            {
                string scaleToken = NormalizeOtherMsgToken(tokens[1]);
                if (float.TryParse(scaleToken, out float scale) && scale > 0)
                {
                    atkWarmingScale = scale;
                }
            }

            if (tokens.Length > 2)
            {
                string pathToken = NormalizeOtherMsgToken(tokens[2]);
                if (!string.IsNullOrEmpty(pathToken))
                {
                    atkWarmingPrefabPath = pathToken;
                }
            }

            return true;
        }

        private string NormalizeOtherMsgToken(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return string.Empty;
            }

            return rawToken.Trim().Trim('[', ']', '"', '\'');
        }
    }
}
