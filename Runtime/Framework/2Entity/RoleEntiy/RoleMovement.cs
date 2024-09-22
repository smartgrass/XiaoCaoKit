using System;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace XiaoCao
{
    public class RoleMovement : RoleComponent<Role>
    {
        public RoleMovement(Role _owner) : base(_owner)
        {
        }

        protected float _tempAnimMoveSpeed;

        protected float velocityY = 0;

        public bool enableGravity = true;

        public float enableGravityTime;

        public float noGravityTimer;

        public float skillNoGravityTimer;

        public float maxAnimMoveSpeed = 1;

        public float newBaseMoveSpeed = 0;

        public CharacterController cc => owner.idRole.cc;
        public Transform tf => owner.idRole.tf;

        public MoveSetting moveSetting => Data_R.moveSetting;

        public Vector3 camForward => CameraMgr.Forword;

        public Vector3 inputDir;
        public bool isLookDir;

        protected bool isGrounded = true;

        //用于控制当前帧是否移动
        public bool isMovingThisFrame = true;

        public override void FixedUpdate()
        {
            //if (owner.IsDie)
            //{
            //    return;
            //}
            if (!owner.IsFree)
            {
                return;
            }

            MoveFixUpdate();
        }

        protected virtual void MoveFixUpdate()
        {
            OnMoveing(inputDir);
        }

        public void SetMoveDir(Vector3 moveDir,float speedRate = 1, bool isLookDir = true)
        {
            inputDir = moveDir;
            this.isLookDir = isLookDir;
            maxAnimMoveSpeed = speedRate;
        }

        public void OnMoveing(Vector3 moveDir, bool isLookDir = true)
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
                RotateByMoveDir(moveDir, moveSetting.rotationLerp);
            }

            float baseSpeed = newBaseMoveSpeed > 0 ? newBaseMoveSpeed : Data_R.moveSetting.baseMoveSpeed;

            float speed = baseSpeed * Data_R.roleState.MoveMultFinal;

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
                owner.Anim?.CrossFade(AnimHash.Idle, 0.05f);
            }
        }


        public void RotateByMoveDir(Vector3 worldMoveDir,float lerp = 1)
        {
            if (worldMoveDir.IsZore())
                return;

            var targetRotation = MathTool.ForwardToRotation(worldMoveDir);
            var startRotation = tf.rotation;
            var rotation = Quaternion.Lerp(startRotation, targetRotation, lerp);
            tf.rotation = rotation;
        }

        private void FixUpdateLockTime()
        {
            if (RoleState.moveLockTime > 0)
            {
                RoleState.moveLockTime -= XCTime.fixedDeltaTime;
            }
            if (RoleState.rotateLockTime > 0)
            {
                RoleState.rotateLockTime -= XCTime.fixedDeltaTime;
            }
        }

        //计算移动动画速度, 同时会影响移速倍率
        void ComputeMoveValue(bool isInput)
        {
            float inputTotal = isInput ? 1 : 0;

            //限制最大移速动画
            inputTotal = Mathf.Clamp(inputTotal, 0, maxAnimMoveSpeed);

            RoleState.animMoveSpeed = Mathf.SmoothDamp(RoleState.animMoveSpeed, inputTotal, ref _tempAnimMoveSpeed, moveSetting.moveSmooth);

            //影响移速倍率
            RoleState.moveAnimMult = MathTool.ValueMapping(RoleState.animMoveSpeed, 0, 1, 1, 1.5f);
        }

        internal void SetUnMoveTime(float t)
        {
            //小于0则取消
            if (t < 0)
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
                DebugGUI.Log("noGravityTimer",noGravityTimer);    
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

    }

}
