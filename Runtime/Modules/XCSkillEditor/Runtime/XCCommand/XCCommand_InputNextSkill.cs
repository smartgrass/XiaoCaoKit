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

        /// <summary>来源技能为2技能(贯穿)时,触发下一段前按方向键输入立刻转向</summary>
        private const string RefaceByInputSkillId = "2";

        private Player0 player0;

        private XCState state;

        /// <summary>最近一次由本指令触发的技能任务信息,便于后续调整起始帧/方向等</summary>
        public TaskInfo nextSkillInfo { get; private set; }

        public override bool IsTargetRoleType(RoleType roleType)
        {
            return roleType == RoleType.Player;
        }

        public override void Init(BaseMsg baseMsg)
        {
            state = XCState.Sleep;
            waitKey = (InputKey)baseMsg.numMsg;
            targetSkill = baseMsg.strMsg;
            if (baseMsg.numMsg > 0)
            {
                minSwitchTime = baseMsg.numMsg;
            }
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
        /// 执行目标技能:与 Role.PlayNextSkill 一致,先打断占用中的任务再播放;
        /// 来源标记供技能侧决定起始帧(1 技能接下一段时从第 8 帧开始)
        /// </summary>
        private void CallNext()
        {
            if (state != XCState.Running)
            {
                return;
            }

            state = XCState.Stopped;

            //已触发次数达到上限:不再接下一段
            if (!CanPlayNextSkill())
            {
                return;
            }

            task.SetNoBusy();

            //技能1(凌空连击)接下一段时:先按方向键输入改角色朝向,再启动技能(技能的 castEuler 取的就是角色朝向)
            if (task.Info.skillId == RefaceByInputSkillId)
            {
                RefaceByInput();
            }

            PlayerControl control = player0.component.control;
            if (control.IsBusy())
            {
                control.BreakAllBusy();
            }

            string callFrom = SkillCallFrom.PlayNextSkillPrefix + task.Info.skillId;
            nextSkillInfo = control.TryPlaySkill(targetSkill, callFrom, task.Info.callCount + 1);
        }

        /// <summary>
        /// NextSkill 是否还有触发余量:当前调用次数 >= 技能配置的最大触发次数时不能再接
        /// </summary>
        private bool CanPlayNextSkill()
        {
            return task.Info.callCount < SkillNextRule.GetMaxCallCount(task.Info.skillId);
        }

        /// <summary>
        /// 立刻改变角色朝向:有方向键输入时按输入方向,没有输入时按索敌方向(都找不到目标则保持原朝向)
        /// </summary>
        private void RefaceByInput()
        {
            //movement.inputDir 是 PlayerInput 每帧写入的世界方向(相机相关)
            UnityEngine.Vector3 inputDir = player0.component.movement.inputDir;
            inputDir.y = 0;
            if (inputDir.sqrMagnitude >= 0.0001f)
            {
                //lerp = 1 表示立即转向,内部会忽略 Y 轴
                player0.transform.RotaToPos(player0.transform.position + inputDir, 1);
                return;
            }

            //没有方向输入:按索敌方向转向(默认 6 米内最近的敌人,全向搜索)
            player0.component.control.AutoDirectToEnemy(1);
        }
    }
}
