namespace XiaoCao
{
    /// <summary>
    /// eName 为 IXCCommand 的子类类名
    /// </summary>
    public class XCCommondEvent : XCEvent
    {
        public IXCCommand Cmd { get; set;}

        private bool HasCmd { get; set; }

        public override void OnTrigger(float startOffsetTime)
        {
            base.OnTrigger(startOffsetTime);
            Cmd = CommandFinder.Inst.GetCommand(eName);
            if (Cmd != null)
            {
                HasCmd = true;
                Cmd.task = this.task;
                Cmd.curEvent = this;
                Cmd.OnTrigger();
            }
        }

        public override void OnUpdateEvent(int frame, float timeSinceTrigger)
        {
            base.OnUpdateEvent(frame, timeSinceTrigger);
            if (HasCmd)
            {
                Cmd.OnUpdate();
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
