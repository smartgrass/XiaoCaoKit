﻿using DG.Tweening;
using System;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace XiaoCao
{
    public class RoleMovement : RoleComponent<Role>
    {
        public RoleMovement(Role _owner) : base(_owner)
        {
            Init();
        }

        public float _tempAnimMoveSmooth;

        protected float velocityY = 0;

        public bool enableGravity = true;

        public float enableGravityTime;

        public float noGravityTimer;

        public float skillNoGravityTimer;

        public float maxAnimMoveSpeed = 1;

        public float overridBaseMoveSpeed = 0;

        public CharacterController cc => owner.idRole.cc;
        public Transform tf => owner.idRole.transform;

        public MoveSettingSo moveSetting => Data_R.moveSetting;

        public Vector3 camForward => CameraMgr.Forword;

        public Vector3 inputDir;
        public Vector3 lastInputDir;

        public bool isLookDir;

        protected bool isGrounded = true;

        //用于控制当前帧是否移动
        public bool isMovingThisFrame = true;

        public void Init()
        {
            owner.Anim.SetFloat(AnimNames.IdleValue, moveSetting.idleValue);
        }

        public override void FixedUpdate()
        {
            if (!owner.IsFree)
            {
                return;
            }

            FixedUpdateMove();
        }

        protected virtual void FixedUpdateMove()
        {
            OnMoveing(inputDir,true);
        }

        public void SetMoveDir(Vector3 moveDir, float speedRate = 1, bool isLookDir = true)
        {
            inputDir = moveDir;
            this.isLookDir = isLookDir;
            maxAnimMoveSpeed = speedRate;
        }

        public void SetLookTarget(Vector3 target)
        {
            Debug.Log($"--- SetLookTarget TODO");
            //需要缓存变量
            //transform.RoateY_Slow(, Data_R.moveSetting.angleSpeed, 8); 

        }

        public void OnMoveing(Vector3 moveDir, bool isLookDir)
        {
            if (RoleState.IsMoveLock)
            {
                moveDir = Vector3.zero;
            }
            if (Data_R.IsBusy || !Data_R.IsStateFree || owner.IsAnimBreak)
            {
                moveDir = Vector3.zero;
            }

            bool isInput = (Mathf.Abs(moveDir.x) + Mathf.Abs(moveDir.z)) > 0;

            ComputeMoveValue(isInput);

            CheckBackToIdle(isInput);

            if (isLookDir)
            {
                RotateByMoveDir(moveDir, moveSetting.rotationLerp * RoleState.angleSpeedMult);
            }

            float baseSpeed = overridBaseMoveSpeed > 0 ? overridBaseMoveSpeed : Data_R.moveSetting.baseMoveSpeed;

            float speed = baseSpeed * Data_R.roleState.moveAnimMult * Data_R.playerAttr.GetValue(EAttr.MoveSpeedMult);

            Vector3 moveDelta = moveDir * speed * XCTime.fixedDeltaTime;

            //速度 v = v + gt
            //处于空中时


            CheckEnableGravity();
            GroundedCheck();

            if (enableGravity && !isGrounded)
            {
                //加速
                velocityY += Data_R.moveSetting.g * XCTime.fixedDeltaTime;
            }
            else
            {
                //衰减
                velocityY = velocityY * Data_R.moveSetting.GOnGroundMult;
                if (velocityY > -0.1f)
                {
                    velocityY = -0.1f;
                }
            }

            moveDelta.y += velocityY * XCTime.fixedDeltaTime;
            if (isMovingThisFrame)
            {
                cc.Move(moveDelta);
            }
            else
            {
                cc.transform.position += moveDelta;
                isMovingThisFrame = true;
            }

            owner.Anim.SetFloat(AnimNames.MoveSpeed, RoleState.animMoveSpeed);

            FixUpdateLockTime();

            Used();
        }

        public void Used()
        {
            lastInputDir = inputDir;
            inputDir = Vector3.zero;
            isLookDir = false;
        }

        public void MoveToImmediate(Vector3 pos)
        {
            cc.transform.position = pos;
            isMovingThisFrame = false;
        }

        void CheckBackToIdle(bool isInput)
        {
            //DebugGUI.Log("skillState", Data_R.skillState);
            //有移动则取消后摇
            if (Data_R.skillState.Data is ESkillState.SkillEnd && isInput)
            {
                Data_R.skillState.SetValue(ESkillState.Idle);
                //Debug.Log($"--- 过渡效果 TODO");
                //owner.Anim?.CrossFade(AnimHash.Idle, 0.1f);
            }
        }

        public void AimToPos(Vector3 pos)
        {
            Vector3 dir = pos - cc.transform.position;
            RotateByMoveDir(dir, 1);
        }

        public void RotateByMoveDir(Vector3 worldMoveDir, float lerp = 1)
        {
            if (worldMoveDir.IsZore())
                return;
            worldMoveDir.y = 0;
            var targetRotation = MathTool.ForwardToRotation(worldMoveDir);
            var startRotation = tf.rotation;
            var rotation = Quaternion.Lerp(startRotation, targetRotation, lerp);
            tf.rotation = rotation;
        }

        
        public void LookToDir(Vector3 worldDir)
        {
            worldDir.y = 0;
            tf.rotation = MathTool.ForwardToRotation(worldDir);
        }

        private void FixUpdateLockTime()
        {
            //平衡
            float unScale = 1;
            if (XCTime.timeScale > 1)
            {
                unScale = 1 / XCTime.timeScale;
            }

            if (RoleState.moveLockTime > 0)
            {
                RoleState.moveLockTime -= XCTime.unscalefixedDeltaTime * unScale;
            }

            if (RoleState.rotateLockTime > 0)
            {
                RoleState.rotateLockTime -= XCTime.unscalefixedDeltaTime * unScale;
            }
        }

        //计算移动动画速度, 同时会影响移速倍率
        void ComputeMoveValue(bool isInput)
        {
            float inputTotal = isInput ? 1 : 0;

            //限制最大移速动画
            inputTotal = Mathf.Clamp(inputTotal, 0, maxAnimMoveSpeed);

            float moveSmooth = moveSetting.moveSmooth;
            if (XCTime.timeScale > 1)
            {
                //加速速成处理
                moveSmooth = moveSmooth / XCTime.timeScale;
            }

            RoleState.animMoveSpeed = Mathf.SmoothDamp(RoleState.animMoveSpeed, inputTotal, ref _tempAnimMoveSmooth, moveSmooth);

            //影响移速倍率
            RoleState.moveAnimMult = MathTool.ValueMapping(RoleState.animMoveSpeed, 0, 1, 1, 1.5f);

            //起始转速偏慢
            RoleState.angleSpeedMult = MathTool.ValueMapping(RoleState.animMoveSpeed, 0, 1, 0f, 2);
            RoleState.angleSpeedMult = Mathf.Min(1, RoleState.angleSpeedMult);
        }

        public void OnDamage(bool isBreak)
        {
            _tempAnimMoveSmooth = 0;
        }

        internal void SetUnMoveTime(float t)
        {
            //小于0则取消
            if (t <= 0)
            {
                RoleState.moveLockTime = 0;
            }
            else
            {
                RoleState.moveLockTime = Mathf.Max(t, RoleState.moveLockTime);
            }

        }

        private void GroundedCheck()
        {
            float GroundedOffset = -0.14f;
            float GroundedRadius = 0.28f;
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(tf.position.x, tf.position.y - GroundedOffset, tf.position.z);

            isGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, Layers.GROUND_MASK | Layers.DEFAULT_MASK, QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (owner.Anim)
            {
                owner.Anim.SetBool(AnimHash.IsGround, isGrounded);
            }

            if (!isGrounded)
            {
                int layerMask = GameSetting.GetTeamGroundCheckMash(owner.team);
                Collider[] cols = Physics.OverlapSphere(spherePosition, GroundedRadius * 2f, layerMask);
                if (cols.Length > 0)
                {
                    for (int i = 0; i < cols.Length; i++)
                    {
                        Vector3 offset = (cc.transform.position - cols[i].transform.position);
                        offset.y = 0;
                        offset = offset.normalized * XCTime.fixedDeltaTime;
                        //排斥角色之间, 防重叠
                        cc.Move(offset);
                        break;
                    }
                }
            }
        }

        private void CheckEnableGravity()
        {
            enableGravity = true;
            if (noGravityTimer > 0)
            {
                enableGravity = false;
                noGravityTimer -= Time.fixedDeltaTime;
            }

            if (skillNoGravityTimer > 0)
            {
                enableGravity = false;
                skillNoGravityTimer -= Time.fixedDeltaTime;
            }
        }
        public void SetSkillNoGravityT(float time)
        {
            skillNoGravityTimer = time;
        }
        public void SetNoGravityT(float time)
        {
            Debug.Log($"--- SetNoGravityT {time} ");
            if (time > noGravityTimer)
            {
                noGravityTimer = time;
            }
        }

        RoleGrappling grappling;
        public void Startgrappling()
        {
            if (grappling == null)
            {
                grappling = new RoleGrappling(this);
            }
            Vector3 addVec = camForward * 15;
            //if (addVec.y < 3) {
                addVec.y = 5;
            //}
            Vector3 targetPos = cc.transform.position + addVec;
            owner.idRole.StartCoroutine(grappling.Grappling(targetPos));
        }
    }

    //钩锁
    public class RoleGrappling {
        public RoleGrappling(RoleMovement roleMovement)
        {
            this.movement = roleMovement;
            owner = movement.owner as Player0;
        }

        private RoleMovement movement;
        public Player0 owner;
        public CharacterController cc=>movement.cc;
        public bool grappling;
        public float speed = 10;

        //TODO 优化
        public System.Collections.IEnumerator Grappling(Vector3 pos)
        {
            //1 跳起
            movement.AimToPos(pos);
            owner.Anim.Play(AnimNames.Jump);
            movement.SetNoGravityT(10);
            movement.SetUnMoveTime(10);
            movement.cc.DOHit(owner.playerData.playerSetting.JumpY, Vector3.zero, 0.2f);

            yield return new WaitForSeconds(0.2f);

            //LookAt TODO 更改瞄准
            CameraMgr.Inst.tempLookAt.position = pos;
            CameraMgr.Inst.aimer.SetAim(CameraMgr.Inst.tempLookAt);
            CameraMgr.Inst.aimer.SetMainWeight(0.7f);

            float moveTime = Vector3.Distance(cc.transform.position, pos) / speed;
            moveTime =Mathf.Clamp(moveTime,0.1f,3);


            //匀速运动 到终点, TODO 改为FixUpdate?
            movement.cc.DoMoveTo(pos, moveTime);

            yield return new WaitForSeconds(moveTime) ;
            //落地动画
            movement.noGravityTimer = 0.0f;
            movement.SetUnMoveTime(-1);
            yield return null;
        }

    }

}
