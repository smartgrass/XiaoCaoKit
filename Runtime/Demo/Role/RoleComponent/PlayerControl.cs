using DG.Tweening;
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
                return;
            }

            if (InputData.skillInput != 0)
            {
                TryCastSkill(InputData.skillInput.ToString());
            }

            if (InputData.inputs[InputKey.NorAck])
            {
                TryNorAck();
            }

            if (InputData.inputs[InputKey.Tab])
            {
                if (BattleData.IsTimeStop)
                {
                    TimeStopMgr.Inst.RecoverTimeSpeed();
                }
                else
                {
                    TimeStopMgr.Inst.StopTimeSpeed(5);
                }
            }
            if (InputData.inputs[InputKey.Focus])
            {
                SwitchFocus();
            }


            if (InputData.inputs[InputKey.Space])
            {
                TryJump();

            }
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

            owner.Anim.Play(AnimNames.Jump);
            JumpTween = cc.DOHit(Data_P.playerSetting.JumpY, Vector3.zero, 0.5f);
            Data_R.movement.SetNoGravityT(Data_P.playerSetting.JumpNoGravityT);

            //if (FindEnemy(out Role findRole))
            //{
            //    Debug.Log($"--- Find Role {findRole.transform.name}");
            //    owner.idRole.StartCoroutine(IEJumpTo(findRole.transform.position));
            //} 
        }


        private System.Collections.IEnumerator IEJumpTo(Vector3 pos)
        {
            //1 跳起, & LookAt
            CameraMgr.Inst.tempLookAt.position = pos;
            CameraMgr.Inst.aimer.SetAim(CameraMgr.Inst.tempLookAt);
            CameraMgr.Inst.aimer.SetMainWeight(0.7f);


            //匀速运动 到终点

            //落地

            yield return null;
        }

        protected override void PreSkillStart(string skillId)
        {
            if (InputData.x != 0 || InputData.y != 0)
            {
                var dir = owner.component.movement.inputDir;
                this.Data_P.movement.RotateByMoveDir(dir, 1);
            }

            if (AnimNames.Roll != skillId)
            {
                DefaultAutoDirect();
            }
        }

        public void TryRoll()
        {
            //判断冷缩
            if (!AtkTimers.IsSkillReady(AnimNames.Roll))
            {
                Debug.Log($"rollId cd {AtkTimers.GetWaitTime(AnimNames.Roll)}");
                return;
            }

            AtkTimers.SetSkillEnterCD(AnimNames.Roll);

            //停止&打断当前动作
            if (IsBusy())
            {
                SetNoBusy();
            }

            RcpPlaySkill(AnimNames.Roll);
        }

        public void TryNorAck()
        {
            if (!IsRoleCanPlaySkill())
                return;

            GameEvent.Send(EventType.AckingNorAck.Int());

            int nextNorAckIndex = AtkTimers.GetNextNorAckIndex();

            Data_P.curNorAckIndex = nextNorAckIndex;

            RcpPlaySkill($"{AnimNames.NorAck}{nextNorAckIndex}");
        }

        public override void DefaultAutoDirect()
        {
            var findRole = RoleMgr.Inst.SearchEnemyRole(owner.gameObject.transform, 3.5f, 30, out float maxScore, owner.team);
            if (findRole != null)
            {
                owner.transform.RotaToPos(findRole.transform.position, 0.4f);
                Debug.Log($"--- findRole RotaToPos {findRole.gameObject}");
            }
            else
            {
                Debug.Log($"--- findRole no");
            }

        }


        //执行使用技能, 一般不直接使用
        public override void RcpPlaySkill(string skillId)
        {
            base.RcpPlaySkill(skillId);
        }


    }

}
