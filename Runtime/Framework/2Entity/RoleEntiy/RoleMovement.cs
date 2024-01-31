using UnityEngine;

namespace XiaoCao
{
    public class RoleMovement : RoleComponent<Role>
    {
        public RoleMovement(Role _owner) : base(_owner) { }

        protected float _tempAnimMoveSpeed;
        public CharacterController cc => owner.idRole.cc;
        public Transform tf => owner.idRole.tf;

        public MoveSetting setting => Data_R.moveSetting;

        public Vector3 camForward => CameraMgr.Forword;

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
            Vector3 moveDir = GetInputMoveDir();

            SetMoveDir(moveDir);
        }

        protected virtual Vector3 GetInputMoveDir()
        {
            if (RoleState.IsMoveLock)
            {
                return Vector3.zero;
            }
            return Data_R.roleState.moveDir;
        }

        public void SetMoveDir(Vector3 moveDir, bool isLookDir = true)
        {
            if (RoleState.IsMoveLock)
            {
                moveDir = Vector3.zero;
            }

            bool isInput = (Mathf.Abs(moveDir.x) + Mathf.Abs(moveDir.z)) > 0;

            ComputeMoveValue(isInput);

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

        void RotateByMoveDir(Vector3 worldMoveDir)
        {
            if (worldMoveDir.IsNaN())
                return;

            var targetRotation = MathTool.ForwardToRotation(worldMoveDir);
            var startRotation = tf.rotation;
            var rotation = Quaternion.Lerp(startRotation, targetRotation, 0.5f);
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

            RoleState.animMoveSpeed = Mathf.SmoothDamp(RoleState.animMoveSpeed, inputTotal, ref _tempAnimMoveSpeed, setting.moveSmooth);

            RoleState.moveAnimMult = MathTool.ValueMapping(RoleState.animMoveSpeed, 0, 1, 1, 1.5f);
        }
    }

}
