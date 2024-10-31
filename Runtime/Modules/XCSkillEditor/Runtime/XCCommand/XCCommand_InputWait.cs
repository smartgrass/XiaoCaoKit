namespace XiaoCao
{
    internal class XCCommand_InputWait : IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }
        public RoleType roleType { get => RoleType.Player; }

        public void Init(BaseMsg baseMsg)
        {

        }

        public void OnFinish(bool hasTrigger)
        {

        }
        
        public void OnTrigger()
        {

        }

        public void OnUpdate(int frame, float timeSinceTrigger)
        {

        }
    }
}
