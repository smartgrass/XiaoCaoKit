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
            else if (msgType is EntityMsgType.InputCheck)
            {
                //this.task.CheckInput()
            }
            baseMsg.state = 0;
            Info.role.ReceiveMsg(msgType, Info.entityId, baseMsg);
        }

        public override void OnUpdateEvent(int frame, float timeSinceTrigger)
        {
            if (msgType is EntityMsgType.InputCheck)
            {
                Player0 player0 = task.Info.role as Player0;

                if (player0.playerData.inputData.inputs[InputKey.NorAck])
                {
                    DebugGUI.Debug("Mouse1", $"---  Input Mouse1");

                    baseMsg.state = 1;
                }

                //if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.J))
                //{
                //   DebugGUI.Debug("Mouse0",$"---  Input Mouse0");
                //}
                //Player0 player0 = this.Info.role as Player0;
                ////this.task.CheckInput()
                //if (player0 != null)
                //{
                //    player0.playerData.inputData.CheckKeyCode
                //}
            }

        }

        public override void OnFinish()
        {
            base.OnFinish();
            if (HasTrigger && isUndoOnFinish)
            {
                if (msgType is EntityMsgType.InputCheck)
                {
                    if (baseMsg.numMsg == 1)
                    {

                    }
                }
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
