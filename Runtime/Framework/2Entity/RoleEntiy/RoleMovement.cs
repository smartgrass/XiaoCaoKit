using System;
using UnityEngine;

namespace XiaoCao
{
    public class RoleMovement : RoleComponent<Role>
    {
        public RoleMovement(Role _owner) : base(_owner) { }

        protected float _tempAnimMoveSpeed;
        public CharacterController cc => owner.idRole.cc;
        public Transform tf => owner.idRole.tf;

        public MoveSetting moveSetting => Data_R.moveSetting;

        public Vector3 camForward => CameraMgr.Forword;

        Vector3 inputDir;

        public override void FixedUpdate()
        {
            if (owner.IsDie)
            {
                return;
            }
            MoveFixUpdate();
        }

        protected virtual void MoveFixUpdate()
        {
            inputDir = GetInputMoveDir();

            Vector3 moveDir = inputDir;

            Data_R.roleState.inputDir = inputDir;

            SetMoveDir(moveDir);
        }

        protected virtual Vector3 GetInputMoveDir()
        {
            return Data_R.roleState.inputDir;
        }

        public void SetMoveDir(Vector3 moveDir, bool isLookDir = true)
        {
            if (RoleState.IsMoveLock)
            {
                moveDir = Vector3.zero;
            }
            if (Data_R.IsBusy || !Data_R.IsFree)
            {
                moveDir = Vector3.zero;
            }

            bool isInput = (Mathf.Abs(moveDir.x) + Mathf.Abs(moveDir.z)) > 0;

            ComputeMoveValue(isInput);

            CheckBackToIdle(isInput);

            if (isLookDir)
            {
                RotateByMoveDir(moveDir);
            }

            Vector3 moveDelta = moveDir * Data_R.moveSetting.baseMoveSpeed * Data_R.roleState.MoveMultFinal * XCTime.fixedDeltaTime;

            //速度 v = v + gt
            float velocityY = 0;
            velocityY = velocityY + Data_R.moveSetting.g * XCTime.fixedDeltaTime;
            moveDelta.y += velocityY * XCTime.fixedDeltaTime;


            cc.Move(moveDelta);
            owner.Anim.SetFloat(AnimNames.MoveSpeed, RoleState.animMoveSpeed);

            FixUpdateLockTime();
        }

        void CheckBackToIdle(bool isInput)
        {
            if (Data_R.skillState is ESkillState.SkillEnd && isInput)
            {
                Data_R.skillState = ESkillState.Idle;
                owner.Anim?.CrossFade(AnimHash.Idle, 0.05f);
            }
        }


        void RotateByMoveDir(Vector3 worldMoveDir)
        {
            if (worldMoveDir.IsNaN())
                return;

            var targetRotation = MathTool.ForwardToRotation(worldMoveDir);
            var startRotation = tf.rotation;
            var rotation = Quaternion.Lerp(startRotation, targetRotation, moveSetting.rotationLerp);
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

            RoleState.animMoveSpeed = Mathf.SmoothDamp(RoleState.animMoveSpeed, inputTotal, ref _tempAnimMoveSpeed, moveSetting.moveSmooth);

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
    }

}
