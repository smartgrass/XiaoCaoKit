namespace XiaoCao
{
    /// <summary>
    /// 等待指定按键输入,触发后释放目标技能。
    /// baseMsg.numMsg = 等待的按键(InputKey 成员,如 InputKey.NorAck);
    /// baseMsg.strMsg = 目标技能 id
    /// </summary>
    internal class XCCommand_InputNextSkill : BaseCommand
    {
        /// <summary>等待的按键</summary>
        private InputKey waitKey;

        /// <summary>触发后要释放的技能 id</summary>
        private string targetSkill;

        private Player0 player0;

        private XCState state;

        public override bool IsTargetRoleType(RoleType roleType)
        {
            return roleType == RoleType.Player;
        }

        public override void Init(BaseMsg baseMsg)
        {
            state = XCState.Sleep;
            waitKey = (InputKey)baseMsg.numMsg;
            targetSkill = baseMsg.strMsg;
        }

        public override void OnTrigger()
        {
            player0 = task.Info.role as Player0;
        }

        public override void OnUpdate(int frame, float timeSinceTrigger)
        {
            //检测输入
            if (state == XCState.Sleep)
            {
                bool[] inputs = player0.playerData.inputData.inputs;
                int keyIndex = (int)waitKey;
                if (keyIndex >= 0 && keyIndex < inputs.Length && inputs[keyIndex])
                {
                    state = XCState.Running;
                }
            }

            //等待到可切换时间点后执行目标技能
            if (state == XCState.Running && timeSinceTrigger > curEvent.LengthTime * minSwitchTime)
            {
                CallNext();
            }
        }

        public override void OnFinish(bool hasTrigger)
        {
            CallNext();
        }

        /// <summary>
        /// 执行目标技能:与 XCCommand_NorAck 一致,先解除当前任务占用再发消息
        /// </summary>
        private void CallNext()
        {
            if (state != XCState.Running)
            {
                return;
            }

            state = XCState.Stopped;
            task.SetNoBusy();

            //PlayNextSkill 需要从 msg 里取技能 id,这里把目标技能带过去
            BaseMsg nextSkillMsg = new BaseMsg() { strMsg = targetSkill };
            player0.ReceiveMsg(EntityMsgType.PlayNextSkill, task.Info.entityId, nextSkillMsg);
        }
    }
}
