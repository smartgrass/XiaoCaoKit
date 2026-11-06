using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// eName 为 IXCCommand 的子类类名
    /// </summary>
    public class XCCommondEvent : XCEvent
    {
        /// <summary>
        /// 玩家设置开关的 key(如 AutoChase); 为空表示不受开关控制, 恒生效
        /// </summary>
        public string disableTag;

        public BaseMsg baseMsg; 

        public string[] otherMsgs;

        public IXCCommand Cmd { get; set;}

        private bool HasCmd { get; set; }

        /// <summary>
        /// 这条指令当前是否该执行: 没填 disableTag 时恒执行,
        /// 填了就按玩家设置里同名开关决定(关闭则不执行)
        /// </summary>
        public bool IsEnabled => string.IsNullOrEmpty(disableTag) || GameSetting.IsSwitchOn(disableTag);

        public override void OnTrigger(float startOffsetTime)
        {
            base.OnTrigger(startOffsetTime);

            //玩家设置把这个开关关掉了: 事件时间轴照常推进, 但不初始化指令
            if (!IsEnabled)
            {
                return;
            }

            Cmd = XCCommandBinder.Inst.GetCommand(eName);
            if (Cmd != null && Cmd.IsTargetRoleType(Info.role.RoleType))
            {
                HasCmd = true;
                Cmd.task = this.task;
                Cmd.curEvent = this;
                Cmd.Init(baseMsg);
                if (Cmd.IsOtherMsg)
                {
                    Cmd.InitOtherMsg(otherMsgs);
                }
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
