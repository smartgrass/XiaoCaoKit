namespace XiaoCao
{
    public class XCMsgEvent : XCEvent
    {
        public EntityMsgType entityMsg;
        public BaseMsg baseMsg;

        public override void OnTrigger(float timeSinceTrigger)
        {
            base.OnTrigger(timeSinceTrigger);
            Info.role.ReceiveMsg(entityMsg, Info.entityId, baseMsg);
        }
    }
}
