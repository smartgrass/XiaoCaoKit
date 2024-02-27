using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace XiaoCao
{
    internal class XCCommand_NorAck : IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }

        private Player0 player0;

        public bool getInput;


        public void Init(BaseMsg baseMsg)
        {

        }

        public void OnTrigger()
        {
            player0 =  task.Info.role as Player0;
            //Debug.Log("XCCommand_101 OnStart");
        }

        public void OnUpdate()
        {
            //需求: 需要一个nextSkll id的一个衔接, 最好是可以能据进度来触发.
            //期间检测不合理, 期间外检测也不合理
            //假如延长检测,并且加入finish时间是否可以?
            ////输入控制->一定拆开 交给代码最灵活 轨道脚本
            ///TriggerRange 检测时间, TranslateTime最小切换时间
            Debug.Log("XCCommand_101 OnUpdate " + task.GetCurFrame);
            if (task.Info.role.RoleType == RoleType.Player)
            {
                if (player0.playerData.inputData.inputs[InputKey.NorAck])
                {
                    Debug.Log($"--- InputKey NorAck");
                    getInput = true;
                }
            }
        }

        public void OnFinish(bool hasTrigger)
        {
            if (hasTrigger && getInput)
            {
                task.SetFinish();
                player0.ReceiveMsg(EntityMsgType.PlayNextNorAck, task.Info.entityId, null);
            }
        }

    }
}
