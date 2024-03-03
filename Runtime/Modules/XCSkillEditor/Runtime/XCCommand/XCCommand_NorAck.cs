using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace XiaoCao
{
    internal class XCCommand_NorAck : IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }

        private Player0 player0;

        public int inputState;

        public float minSwitchTime = 0.8f;

        public void Init(BaseMsg baseMsg)
        {
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
            if (inputState == 0)
            {
                if (player0.playerData.inputData.inputs[InputKey.NorAck])
                {
                    inputState = 1;
                }
            }

            //等待触发
            if (inputState == 1)
            {
                if (timeSinceTrigger > curEvent.LengthTime * minSwitchTime)
                {
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
            if (inputState == 1)
            {
                inputState = 2;
                task.SetNoBusy();
                player0.ReceiveMsg(EntityMsgType.PlayNextNorAck, task.Info.entityId, null);
            }
        }
    }
}
