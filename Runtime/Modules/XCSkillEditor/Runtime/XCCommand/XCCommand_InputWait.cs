namespace XiaoCao
{
    internal class XCCommand_InputWait : BaseCommand
    {
        public override bool IsTargetRoleType(RoleType roleType)
        {
            return roleType == RoleType.Player;
        }

        public override void Init(BaseMsg baseMsg)
        {

        }

        public override void OnFinish(bool hasTrigger)
        {

        }
        
        public override void OnTrigger()
        {

        }

        public override void OnUpdate(int frame, float timeSinceTrigger)
        {

        }
    }
}
