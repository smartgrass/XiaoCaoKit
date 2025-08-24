using DG.Tweening;
using EasyUI.Helpers;
using TEngine;
using UnityEngine;

namespace XiaoCao
{

    public class PlayerControl : RoleControl<Player0>
    {
        public PlayerControl(Player0 _owner) : base(_owner) { }
        public PlayerData0 Data_P => owner.playerData;
        public PlayerInputData InputData => owner.playerData.inputData;
        public PlayerAtkTimer AtkTimers => owner.component.atkTimers;
        public Tween JumpTween { get; set; }

        public string RollName => Data_P.playerSetting.rollSkillId;

        public override void Update()
        {
            if (owner.IsDie)
            {
                OnDeadUpdate();
                return;
            }
            owner.CheckBreakUpdate();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (InputData.inputs[InputKey.LeftShift])
            {
                TryRoll();
                Data_P.norAckCache = false;
                return;
            }

            if (InputData.skillInput >= 0)
            {
                ///<see cref="SkillDataSo"/>
                string skillId = Data_P.GetBarSkillId(InputData.skillInput);
                TryCastSkill(skillId);
                Data_P.norAckCache = false;
            }

            if (InputData.inputs[InputKey.NorAck] || Data_P.norAckCache)
            {
                TryNorAck();
            }

            //Editor Debug用
            if (InputData.inputs[InputKey.Tab])
            {
                if (BattleData.IsTimeStop)
                {
                    TimeStopMgr.Inst.RecoverTimeSpeed();
                }
                else
                {
                    TimeStopMgr.Inst.StopTimeSpeed();
                }
                owner.component.atkTimers.ClearAllCd();
            }
            if (InputData.inputs[InputKey.Focus])
            {
                SwitchFocus();
            }


            if (InputData.inputs[InputKey.Space])
            {
                TryJump();
            }


            //CheckInputAction
        }

        public void TryCastSkill(string skillId)
        {
            if (!IsRoleCanPlaySkill())
            {
                return;
            }
            //判断冷缩
            if (!AtkTimers.IsSkillReady(skillId))
            {
                return;
            }
            AtkTimers.SetSkillEnterCD(skillId);

            RcpPlaySkill(skillId);
        }


        private void SwitchFocus()
        {
            bool isOn = ConfigMgr.LocalSetting.GetBoolValue(LocalizeKey.LockCam);
            ConfigMgr.LocalSetting.SetBoolValue(LocalizeKey.LockCam, !isOn);
        }

        public void TryJump()
        {
            if (!IsRoleCanPlaySkill())
            {
                return;
            }

            if (JumpTween != null && JumpTween.IsPlaying())
            {
                return;
            }
            //暂时关闭
            //Data_R.movement.Startgrappling();

            owner.Anim.CrossFade(AnimNames.Jump, 0.15f);
            JumpTween = cc.DOHit(Data_P.playerSetting.JumpY, Vector3.zero, 0.5f);
            Data_R.movement.SetNoGravityT(Data_P.playerSetting.JumpNoGravityT);
        }


        protected override void PreSkillStart(string skillId)
        {
            if (InputData.X != 0 || InputData.Y != 0)
            {
                var dir = owner.component.movement.inputDir;
                this.Data_R.movement.RotateByMoveDir(dir, 1);
            }
            //Time.frameCount

            if (RollName != skillId)
            {
                DefaultAutoDirect();
            }
        }

        public void TryRoll()
        {
            if (string.IsNullOrEmpty(RollName))
            {
                Debug.LogError("--- rollName null");
                return;
            }

            //判断冷缩
            if (!AtkTimers.IsSkillReady(RollName))
            {
                Debug.Log($"rollId cd {AtkTimers.GetWaitTime(RollName)}");
                //UIMgr.PopToast($"rollId is in cd {AtkTimers.GetWaitTime(AnimNames.Roll).ToString("N2")}s");
                return;
            }

            AtkTimers.SetSkillEnterCD(RollName);

            //停止&打断当前动作
            if (IsBusy())
            {
                BreakAllBusy();
            }

            RcpPlaySkill(RollName);
        }

        public override void SetNoBusy(int runnerId)
        {
            foreach (var item in runnerList)
            {
                if (item.GetInstanceID() == runnerId)
                {
                    item.OnNoBusy();
                }
            }
        }

        public void TryNorAck()
        {
            if (!Data_R.IsStateFree)
            {
                return;
            }
            //如处于busy中,排除处于平a中
            if (IsBusy() && !IsOnNorAck())
            {
                return;
            }
            float norAckCdTime = Data_P.playerSetting.norAckCdTimes.GetArrayValue(Data_P.curNorAckIndex) / XCTime.timeScale;
            bool isNorAckCdFinsh = Time.time - Data_P.lastNorAckTime > norAckCdTime;
            if (!isNorAckCdFinsh)
            {
                if (IsOnNorAck() && Time.time - Data_P.lastNorAckTime > norAckCdTime * 0.5f)
                {
                    Data_P.norAckCache = true;
                }
                return;
            }

            if (IsOnNorAck() && norAckTaskRunner.IsBusy)
            {
                norAckTaskRunner.OnNoBusy();
            }
            NorAck();
        }

        private XCTaskRunner norAckTaskRunner;

        private void NorAck()
        {
            int nextNorAckIndex = AtkTimers.GetNextNorAckIndex();

            Data_P.curNorAckIndex = nextNorAckIndex;

            Data_P.lastNorAckTime = Time.time;

            Data_P.norAckCache = false;

            RcpPlaySkill(GetNorAtkName(nextNorAckIndex));

            Debug.Log($"--- NorAck {AnimNames.NorAck}{nextNorAckIndex}");

            norAckTaskRunner = runnerList[runnerList.Count - 1];

            Debug.Log($"--- runner {norAckTaskRunner.gameObject.GetInstanceID()}");
        }

        private string GetNorAtkName(int num)
        {
            if (owner.raceId != 0)
            {
                return Data_P.GetBarSkillId(0);
            }

            return $"{AnimNames.NorAck}{num}";
        }



        private bool IsOnNorAck()
        {
            return Data_R.tempCurSkillId.StartsWith(AnimNames.NorAck);
        }

        //public override void DefaultAutoDirect()
        //{

        //}


        //执行使用技能, 一般不直接使用
        public override void RcpPlaySkill(string skillId)
        {
            GameEvent.Send<int, string>(EGameEvent.PlayerPlaySkill.Int(), owner.id, skillId);
            base.RcpPlaySkill(skillId);
        }


    }

}

