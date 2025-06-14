
using UnityEngine;

namespace XiaoCao
{

    internal class XCCommand_NorAck : IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }
        public RoleType roleType { get => RoleType.Player; }

        private Player0 player0;

        public XCState state;

        public float minSwitchTime = 0.8f;

        public void Init(BaseMsg baseMsg)
        {
            state = XCState.Sleep;
            if (baseMsg.numMsg > 0)
            {
                minSwitchTime = baseMsg.numMsg;
            }

        }

        public void OnTrigger()
        {
            player0 = task.Info.role as Player0;
            //Debug.Log("XCCommand_101 OnStart");
        }

        public void OnUpdate(int frame, float timeSinceTrigger)
        {
            //检测触发
            if (state == XCState.Sleep)
            {
                if (player0.playerData.inputData.inputs[InputKey.NorAck])
                {
                    state = XCState.Running;
                }
            }

            //等待触发
            if (state == XCState.Running)
            {
                if (timeSinceTrigger > curEvent.LengthTime * minSwitchTime)
                {
                    Debug.Log($"--- timeSinceTrigger {timeSinceTrigger} {curEvent.LengthTime * minSwitchTime}");
                    CallNext();
                }
            }


        }

        public void OnFinish(bool hasTrigger)
        {
            CallNext();
        }

        //执行触发
        private void CallNext()
        {
            if (state == XCState.Running)
            {
                //如果触发成功,需要关闭检测
                state = XCState.Stopped;
                task.SetNoBusy();
                player0.ReceiveMsg(EntityMsgType.PlayNextNorAck, task.Info.entityId, null);
            }
        }
    }
}
