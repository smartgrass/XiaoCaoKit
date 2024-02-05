using UnityEngine;

namespace XiaoCao
{
    internal class XCCommand_101 : IXCCommand
    {
        public XCTask task { get;set; }

        public void OnExit()
        {
            Debug.Log("XCCommand_101 OnExit");
        }

        public void OnStart()
        {
            Debug.Log("XCCommand_101 OnStart");
        }

        public void OnUpdate()
        {
            Debug.Log("XCCommand_101 OnUpdate " + task.GetCurFrame);
            if (task.Info.role.RoleType == RoleType.Player)
            {
                Player0 player0 = task.Info.role as Player0;

                if (player0.playerData.inputData.inputs[InputKey.NorAck])
                {
                    Debug.Log($"--- InputKey NorAck");

                    //假设平a后是切换技能
                    task.SetFinish();
                    player0.ReceiveMsg(EntityMsgType.PlayNextSkill,task.Info.entityId ,111);
                }
            }
        }
    }
}
