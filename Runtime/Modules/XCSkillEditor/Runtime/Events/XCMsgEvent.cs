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
            if (msgType == EntityMsgType.SkillFinish_Num)
            {
                task.SetFinish();
            }
            baseMsg.state = 0;
            Info.role.ReceiveMsg(msgType, Info.entityId, baseMsg);
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


    public class XCCommondEvent : XCEvent
    {
        public IXCCommand cmd;

        private bool hasCmd = false;

        public override void OnTrigger(float startOffsetTime)
        {
            base.OnTrigger(startOffsetTime);
            cmd = CommandFinder.Inst.GetCommand(eName);
            if (cmd != null)
            {
                hasCmd = true;
                cmd.task = this.task;
                cmd.curEvent = this;
                cmd.OnTrigger();
            }
        }

        public override void OnUpdateEvent(int frame, float timeSinceTrigger)
        {
            base.OnUpdateEvent(frame, timeSinceTrigger);
            if (hasCmd)
            {
                cmd.OnUpdate();
            }
        }

        public override void OnFinish()
        {
            base.OnFinish();
            if (hasCmd)
            {
                cmd.OnFinish(HasTrigger);
            }
        }
    }
}
