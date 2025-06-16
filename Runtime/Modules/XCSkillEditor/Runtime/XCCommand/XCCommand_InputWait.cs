namespace XiaoCao
{
    internal class XCCommand_InputWait : IXCCommand
    {
        public XCTask task { get; set; }
        public XCCommondEvent curEvent { get; set; }
        public bool IsTargetRoleType(RoleType roleType)
        {
            return roleType == RoleType.Player;
        }

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
