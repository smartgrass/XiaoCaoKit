using DG.Tweening;
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

        #region 掉出地图保护

        /// <summary>位置持续下降超过该时间(秒)即判定为意外坐标</summary>
        public static float fallOutTime = 7f;

        /// <summary>判定为意外坐标后最多回退次数, 超过则按阵亡处理</summary>
        public static int maxFallOutRecover = 2;

        //连续下落的累计时间
        private float _fallTimer;

        //已回退次数, 正常落地后清零
        private int _fallOutRecoverCount;

        //最近一次可站立的坐标(落地/出生/传送时记录), 掉出地图时回退到这里
        private Vector3 _lastSafePos;
        private bool _hasSafePos;

        //上一帧的高度, 用于判断是否真的在下落
        private float _lastPosY;
        private bool _fallCheckInited;

        #endregion

        public bool enableGravity = true;

        public float enableGravityTime;

        public float noGravityTimer;

        public float skillNoGravityTimer;

        public float maxAnimMoveSpeed = 1;

        public float curBaseMoveSpeed = 0;

        public CharacterController cc => owner.idRole.cc;
        public Transform transform => owner.idRole.transform;

        public MoveSettingSo moveSetting => Data_R.moveSetting;

        public Vector3 camForward => CameraMgr.Forword;

        public Vector3 inputDir;
        public Vector3 lastInputDir;

        public bool isLookMoveDir = true;

        protected bool isGrounded = true;

        //用于控制当前帧是否移动
        public bool isMovingThisFrame = true;

        private Transform tempLookAt;

        public void Init()
        {
            ApplyIdleValue();
            curBaseMoveSpeed = moveSetting.baseMoveSpeed;
        }

        /// <summary>
        /// 把 MoveSetting 的 idleValue 写入动画机;
        /// 换 body 后 Animator 是新实例, 参数会回到默认值, 需要重新写入。
        /// </summary>
        public void ApplyIdleValue()
        {
            owner.Anim.SetFloat(AnimNames.IdleValue, moveSetting.idleValue);
        }

        public override void FixedUpdate()
        {
            //掉出地图保护放在最前面: 处于技能/受击等非自由状态时也要检测, 否则会一直往下掉
            CheckFallOut();

            if (!owner.IsFree)
            {
                return;
            }

            FixedUpdateMove();
        }

        protected virtual void FixedUpdateMove()
        {
            OnMoveing(inputDir, isLookMoveDir);
        }

        public void SetMoveDir(Vector3 moveDir, float speedRate = 1, bool isLookDir = true)
        {
            inputDir = moveDir;
            maxAnimMoveSpeed = Mathf.Abs(speedRate);
            isLookMoveDir = isLookDir;
        }

        public void SetLookTarget(Transform target)
        {
            tempLookAt = target;
        }

        public void OnMoveing(Vector3 moveDir, bool isLookMoveDir)
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

            bool lookMoveDir = isLookMoveDir || (!tempLookAt);

            ComputeMoveValue(isInput, lookMoveDir, moveDir);


            if (lookMoveDir)
            {
                RotateByMoveDir(moveDir, moveSetting.rotationLerp * RoleState.angleSpeedMult);
            }
            else
            {
                RotateLootAtWithAnim(tempLookAt, moveSetting.rotationLerp * RoleState.angleSpeedMult);
            }

            //有移动则取消后摇
            if (Data_R.skillState.Data is ESkillState.SkillEnd)
            {
                if (owner.IsPlayer)
                {
                    DebugGUI.Log("moveDir", moveDir.ToString(), isInput, lookMoveDir);
                }
            }

            if (Data_R.skillState.Data is ESkillState.SkillEnd)
            {
                if (isInput)
                {
                    Data_R.skillState.SetValue(ESkillState.Idle);
                    //Debug.Log($"--- 过渡效果 TODO");
                    owner.Anim?.CrossFade(AnimHash.Idle, 0.1f);
                }
            }

            CheckBackToIdle(isInput);


            float baseSpeed = curBaseMoveSpeed;
            if (Data_R.roleState.animMoveSpeed < 0.1f)
            {
                baseSpeed *= moveSetting.backMoveSpeedMult;
            }


            float speed = baseSpeed * Data_R.roleState.moveAnimMult * Data_R.RoleAttr.GetValue(EAttr.MoveSpeedMult);

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
#if UNITY_EDITOR
            if (DebugSetting.PauseFrame + 1 == Time.frameCount)
            {
                isMovingThisFrame = true;
            }
#endif
            if (isMovingThisFrame)
            {
                cc.Move(moveDelta);
            }
            else
            {
                cc.transform.position += moveDelta;
                isMovingThisFrame = true;
            }

            FixUpdateLockTime();
        }

        public void Used()
        {
            lastInputDir = inputDir;
            inputDir = Vector3.zero;
            isLookMoveDir = false;
        }

        public void MoveToImmediate(Vector3 pos)
        {
            cc.transform.position = pos;
            isMovingThisFrame = false;

            //出生/传送门/复活/掉出地图回退都走这里, 顺手记成安全点
            MarkSafePos(pos);
        }

        /// <summary>
        /// 记录一个可以站立的坐标, 掉出地图时回退到这里
        /// </summary>
        private void MarkSafePos(Vector3 pos)
        {
            _lastSafePos = pos;
            _hasSafePos = true;
            _lastPosY = pos.y;
            _fallTimer = 0f;
            _fallCheckInited = true;
        }

        /// <summary>
        /// 掉出地图保护: 位置持续下降超过 fallOutTime 秒即判定为意外坐标。
        /// 先回退到最近的安全坐标, 多次仍无法落地则按阵亡处理,
        /// 避免敌人无限下落导致敌人组永远无法结算、关卡卡死。
        /// </summary>
        private void CheckFallOut()
        {
            Vector3 pos = transform.position;

            if (!_fallCheckInited)
            {
                MarkSafePos(pos);
            }

            float deltaY = pos.y - _lastPosY;
            _lastPosY = pos.y;

            //配置为 0 表示关闭保护
            if (fallOutTime <= 0f)
            {
                _fallTimer = 0f;
                return;
            }

            if (owner.IsDie)
            {
                _fallTimer = 0f;
                return;
            }

            //站住了: 清空下落计时和回退计数
            if (isGrounded)
            {
                _fallTimer = 0f;
                _fallOutRecoverCount = 0;
                return;
            }

            //没有持续下降(上升/悬停/贴地被卡住)不算下落, 避免下坡、跳跃上升被误判
            if (deltaY > -0.0001f)
            {
                _fallTimer = 0f;
                return;
            }

            _fallTimer += XCTime.fixedDeltaTime;
            if (_fallTimer < fallOutTime)
            {
                return;
            }

            RecoverFromFallOut(pos);
        }

        /// <summary>
        /// 意外坐标处理: 回退到安全点, 超过重试次数则判定阵亡
        /// </summary>
        private void RecoverFromFallOut(Vector3 fallPos)
        {
            _fallTimer = 0f;
            _fallOutRecoverCount++;

            string roleName = owner.gameObject ? owner.gameObject.name : owner.id.ToString();

            if (_fallOutRecoverCount > maxFallOutRecover)
            {
                Debuger.LogWarning($"--- {roleName} 连续下落超时 {_fallOutRecoverCount} 次, 判定为意外坐标, 按阵亡处理");
                owner.OnDie(new AtkInfo());
                return;
            }

            Vector3 target = _hasSafePos ? _lastSafePos : fallPos + Vector3.up * 2f;

            //安全点下方没有地面时(平台已消失等), 至少落到找到的地面上, 找不到才保持原点
            if (TryFindGround(target, out Vector3 groundPos))
            {
                target = groundPos;
            }

            Debuger.LogWarning($"--- {roleName} 下落超过 {fallOutTime}s, 判定为意外坐标, 回退到 {target} (第 {_fallOutRecoverCount} 次)");

            velocityY = 0f;
            MoveToImmediate(target);
        }

        /// <summary>
        /// 从指定坐标向下找地面, 找不到返回 false
        /// </summary>
        private static bool TryFindGround(Vector3 from, out Vector3 groundPos)
        {
            const float maxDistance = 50f;
            Vector3 origin = from + Vector3.up * 2f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance,
                    Layers.GROUND_MASK | Layers.DEFAULT_MASK, QueryTriggerInteraction.Ignore))
            {
                groundPos = hit.point + Vector3.up * 0.1f;
                return true;
            }

            groundPos = from;
            return false;
        }

        void CheckBackToIdle(bool isInput)
        {
            //DebugGUI.Log("skillState", Data_R.skillState);
            //有移动则取消后摇
        }


        public void RotateByMoveDir(Vector3 worldMoveDir, float lerp = 1)
        {
            if (worldMoveDir.IsZore())
                return;
            worldMoveDir.y = 0;
            var targetRotation = MathTool.ForwardToRotation(worldMoveDir);
            var startRotation = transform.rotation;
            var rotation = Quaternion.Lerp(startRotation, targetRotation, lerp);
            transform.rotation = rotation;
        }

        private void RotateLootAtWithAnim(Transform lookAtTf, float lerp = 1)
        {
            if (lookAtTf)
            {
                Vector3 dir = lookAtTf.position - transform.position;
                RotateByMoveDir(dir, lerp);
            }
        }

        public void RotateByTargetPos(Vector3 pos, float lerp = 1)
        {
            Vector3 dir = pos - cc.transform.position;
            RotateByMoveDir(dir, lerp);
        }

        public void LookToDir(Vector3 worldDir)
        {
            worldDir.y = 0;
            transform.rotation = MathTool.ForwardToRotation(worldDir);
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

        public float speedDownTime;
        public float speedDownStep = 1;

        /// <summary>
        /// 减速倍率: 1 正常, 0 完全停住。
        /// 只由 speedDownTime 驱动, 不再改写 maxAnimMoveSpeed。
        /// 原先直接扣 maxAnimMoveSpeed, 攻击结束时会把这个"上限"永久压成 0,
        /// 之后哪怕重新设置移动指令也救不回来, 表现为 Attack 后倒退 animMoveSpeed 一直是 0。
        /// </summary>
        private float speedDownRate = 1f;

        /// <summary>
        /// 倒退判定角度: 移动方向与"看向目标方向"的夹角超过该值即视为倒退,
        /// animMoveSpeed 取负, 动画机混合树(-0.5 段为倒放走路)播放倒退动画。
        /// </summary>
        public float backMoveAngle = 90f;

        /// <summary>
        /// 倒退判定的退出迟滞, 避免角度在阈值附近抖动导致前进/倒退动画来回切。
        /// </summary>
        public float backAngleHysteresis = 30f;

        //当前是否判定为倒退移动
        private bool _isBackMove;

        /// <summary>
        /// 清除减速残留, 攻击/硬直结束时调用, 避免进入下一阶段后移速仍被压着。
        /// </summary>
        public void ResetSpeedDown()
        {
            speedDownTime = 0;
            speedDownRate = 1f;
        }

        //计算移动动画速度, 同时会影响移速倍率
        void ComputeMoveValue(bool isInput, bool lookMoveDir, Vector3 moveDir)
        {
            float inputTotal = isInput ? 1 : 0;

            //减速处理: 只压倍率, 不动 maxAnimMoveSpeed
            if (speedDownTime > 0)
            {
                speedDownTime -= Time.deltaTime;
                speedDownRate = Mathf.Max(0, speedDownRate - speedDownStep * Time.deltaTime);
                if (speedDownTime < 0)
                {
                    speedDownTime = 0;
                }
            }
            else if (speedDownRate < 1)
            {
                //没有减速需求了立刻恢复, 否则会一直卡在 0
                speedDownRate = 1;
            }

            //限制最大移速动画
            inputTotal = Mathf.Clamp(inputTotal, 0, maxAnimMoveSpeed) * speedDownRate;

            //减速时同步刹住位移
            if (isInput && speedDownRate < 1 && !inputDir.IsZore())
            {
                inputDir = inputDir.normalized * (maxAnimMoveSpeed * speedDownRate);
            }

            float moveSmooth = moveSetting.moveSmooth;
            if (XCTime.timeScale > 1)
            {
                //加速速成处理
                moveSmooth = moveSmooth / XCTime.timeScale;
            }
            
            float targetAnimMoveSpeed = inputTotal;


            if (!lookMoveDir && tempLookAt)
            {
                Vector3 lookDir = tempLookAt.position - transform.position;
                float angle = MathTool.GetDirectionSinAngle(lookDir, moveDir);

                //钝角即倒退, 带迟滞, 避免角度抖动时前进/倒退来回切
                float absAngle = Mathf.Abs(angle);
                if (_isBackMove)
                {
                    if (absAngle < backMoveAngle - backAngleHysteresis)
                    {
                        _isBackMove = false;
                    }
                }
                else if (absAngle > backMoveAngle)
                {
                    _isBackMove = true;
                }

                if (_isBackMove && targetAnimMoveSpeed > 0)
                {
                    targetAnimMoveSpeed = -targetAnimMoveSpeed;
                }
            }
            else
            {
                _isBackMove = false;
            }


            //幅值做平滑, 符号直接跟随目标: 正负切换不经过 0。
            //原先对带符号的值直接 SmoothDamp, 前进<->倒退切换时会长时间停在 0 附近,
            //表现就是"倒退时 MoveSpeed 接近 0, 不播走路动画"。
            float curMag = Mathf.Abs(RoleState.animMoveSpeed);
            float targetMag = Mathf.Abs(targetAnimMoveSpeed);
            float smoothMag = Mathf.SmoothDamp(curMag, targetMag, ref _tempAnimMoveSmooth, moveSmooth);
            RoleState.animMoveSpeed = targetAnimMoveSpeed < 0 ? -smoothMag : smoothMag;


            if (!owner.IsPlayer)
            {
                DebugGUI.Log("animMoveSpeed", Data_R.roleState.animMoveSpeed.ToString("N1"),
                    targetAnimMoveSpeed.ToString("N1"));
            }

            float absAnimMoveSpeed = Mathf.Abs(RoleState.animMoveSpeed);

            //影响移速倍率
            RoleState.moveAnimMult = MathTool.ValueMapping(absAnimMoveSpeed, 0, 1, 1, 1.5f);

            //起始转速偏慢
            RoleState.angleSpeedMult = MathTool.ValueMapping(absAnimMoveSpeed, 0, 1, 0f, 2);
            RoleState.angleSpeedMult = Mathf.Min(1, RoleState.angleSpeedMult);


            owner.Anim.SetFloat(AnimNames.MoveSpeed, RoleState.animMoveSpeed);
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

        internal void SetUnRotateTime(float t)
        {
            if (t <= 0)
            {
                RoleState.rotateLockTime = 0;
            }
            else
            {
                RoleState.rotateLockTime = Mathf.Max(t, RoleState.rotateLockTime);
            }
        }


        private void GroundedCheck()
        {
            float GroundedOffset = -0.14f;
            float GroundedRadius = 0.28f;
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);

            isGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, Layers.GROUND_MASK | Layers.DEFAULT_MASK,
                QueryTriggerInteraction.Ignore);

            //落在地面上时更新安全点, 掉出地图后回退到这里
            if (isGrounded)
            {
                _lastSafePos = transform.position;
                _hasSafePos = true;
            }

            // update animator if using character
            if (owner.Anim)
            {
                owner.Anim.SetBool(AnimHash.IsGround, isGrounded);
            }

            if (!isGrounded)
            {
                int layerMask = XCSetting.GetTeamInverseLayerMask(owner.team);
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
                        if (Application.isEditor)
                        {
                            if (!offset.IsZore())
                            {
                                Debug.Log($"-- {owner.gameObject} offsetMove {offset} {cols[i].gameObject}");
                            }
                        }


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
            //Debug.Log($"--- SetNoGravityT {time} ");
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
    public class RoleGrappling
    {
        public RoleGrappling(RoleMovement roleMovement)
        {
            this.movement = roleMovement;
            owner = movement.owner as Player0;
        }

        private RoleMovement movement;
        public Player0 owner;
        public CharacterController cc => movement.cc;
        public bool grappling;
        public float speed = 10;

        //TODO 优化
        public System.Collections.IEnumerator Grappling(Vector3 pos)
        {
            //1 跳起
            movement.RotateByTargetPos(pos);
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
            moveTime = Mathf.Clamp(moveTime, 0.1f, 3);


            //匀速运动 到终点, TODO 改为FixUpdate?
            movement.cc.DoMoveTo(pos, moveTime);

            yield return new WaitForSeconds(moveTime);
            //落地动画
            movement.noGravityTimer = 0.0f;
            movement.SetUnMoveTime(-1);
            yield return null;
        }
    }
}