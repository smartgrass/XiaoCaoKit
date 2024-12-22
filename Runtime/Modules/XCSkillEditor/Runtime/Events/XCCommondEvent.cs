namespace XiaoCao
{
    /// <summary>
    /// eName 为 IXCCommand 的子类类名
    /// </summary>
    public class XCCommondEvent : XCEvent
    {
        public BaseMsg baseMsg;

        public IXCCommand Cmd { get; set;}

        private bool HasCmd { get; set; }

        public override void OnTrigger(float startOffsetTime)
        {
            base.OnTrigger(startOffsetTime);
            Cmd = XCCommandBinder.Inst.GetCommand(eName);
            if (Cmd != null && Cmd.roleType == Info.role.RoleType)
            {
                HasCmd = true;
                Cmd.task = this.task;
                Cmd.curEvent = this;
                Cmd.Init(baseMsg);
                Cmd.OnTrigger();
            }
        }

        public override void OnUpdateEvent(int frame, float timeSinceTrigger)
        {
            base.OnUpdateEvent(frame, timeSinceTrigger);
            if (HasCmd)
            {
                Cmd.OnUpdate(frame, timeSinceTrigger);
            }
        }

        public override void OnFinish()
        {
            base.OnFinish();
            if (HasCmd)
            {
                Cmd.OnFinish(HasTrigger);
            }
        }
    }
}
