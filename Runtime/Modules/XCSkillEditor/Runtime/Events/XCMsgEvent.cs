namespace XiaoCao
{
    public class XCMsgEvent : XCEvent
    {
        public EntityMsgType entityMsg;
        public BaseMsg baseMsg;

        public override void OnTrigger(float timeSinceTrigger)
        {
            base.OnTrigger(timeSinceTrigger);
            if (entityMsg == EntityMsgType.SkillFinish_Num)
            {
                task.SetFinish();
            }

            Info.role.ReceiveMsg(entityMsg, Info.entityId, baseMsg);
        }
    }
}
