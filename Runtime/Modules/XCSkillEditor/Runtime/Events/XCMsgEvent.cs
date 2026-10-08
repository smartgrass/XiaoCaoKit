using UnityEngine;
using UnityEngine.UIElements;

namespace XiaoCao
{
    public class XCMsgEvent : XCEvent
    {
        public EntityMsgType msgType;
        public BaseMsg baseMsg;
        public bool isUndoOnFinish;

        public override void OnTrigger(float timeSinceTrigger)
        {
            base.OnTrigger(timeSinceTrigger);
            if (msgType == EntityMsgType.SetNoBusy)
            {
                task.SetNoBusy();
            }
            else if (msgType == EntityMsgType.ObjectUnFollow)
            {
                UnFollowNodeObject();
            }

            baseMsg.state = 0;
            Info.role.ReceiveMsg(msgType, Info.entityId, baseMsg);
        }

        /// <summary>
        /// 让当前节点的物体脱离父节点(不再跟随角色)
        /// </summary>
        private void UnFollowNodeObject()
        {
            if (task.ObjectData == null)
            {
                Debuger.LogWarning($"--- ObjectUnFollowPlayer 只能放在节点(子技能)时间轴上 {Info.skillId}");
                return;
            }

            task.ObjectData.UnFollowParent();
        }



        public override void OnFinish()
        {
            base.OnFinish();
            if (HasTrigger && isUndoOnFinish)
            {
                baseMsg.state = 1;
                Info.role.ReceiveMsg(msgType, Info.entityId, baseMsg);
            }
        }
    }
}
