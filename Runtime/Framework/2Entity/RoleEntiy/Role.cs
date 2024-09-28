
using cfg;
using NaughtyAttributes;
using System;
using TEngine;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace XiaoCao
{
    [TypeLabel(typeof(RoleTagCommon))]
    public abstract class Role : HealthBehavior
    {
        public Role()
        {
            DataCreat();
        }

        public abstract void DataCreat();

        public abstract RoleType RoleType { get; }

        public bool IsPlayer { get =>  RoleType == RoleType.Player; }

        //roleData
        public RoleData roleData;

        public int Level{get => roleData.playerAttr.lv; set => roleData.playerAttr.lv = value;}
        public override int Hp { get => roleData.playerAttr.hp; set => roleData.playerAttr.hp = value; }
        public override int MaxHp { get => roleData.playerAttr.maxHp; set => roleData.playerAttr.maxHp = value; }

        public float ShowArmorPercentage => roleData.breakData.ShowPercentage;

        [ShowNativeProperty]
        public override bool IsDie => roleData.bodyState == EBodyState.Dead;
        public bool IsFree => roleData.IsStateFree;

        public bool IsAnimBreak => Anim.GetCurrentAnimatorStateInfo(0).IsTag("Break");

        ///职业,种族: 每一个角色和敌人都有对应的 raceId
        ///<see cref="RaceIdSetting"/>
        public int raceId;

        public RaceInfo raceInfo;

        /// 绑定的模型文件
        public int prefabId;

        public int bodyId;

        //势力,相同为友军
        public int team;

        public GameObject body;

        public IdRole idRole;


        #region Get
        internal Animator Anim => idRole.animator;
        public bool isBodyCreated => body != null;
        #endregion

        //AI控制
        public bool IsAiOn;

        public virtual void CreateIdRole(int prefabId)
        {
            this.prefabId = prefabId;
            string baseRole = XCPathConfig.GetIdRolePath(RoleType, prefabId);
            GameObject idRoleGo = ResMgr.LoadInstan(baseRole, PackageType.ExtraPackage);
            idRole = idRoleGo.transform.GetComponent<IdRole>();
            idRole.id = id;
            raceId = idRole.raceId;
            idRoleGo.tag = RoleType == RoleType.Enemy ? Tags.ENEMY : Tags.PLAYER;
            BindGameObject(idRole.gameObject);
            raceInfo = ConfigMgr.LoadSoConfig<RaceInfoSettingSo>().GetOrFrist(raceId);
#if UNITY_EDITOR
            var testDraw = idRoleGo.AddComponent<Test_GroundedDrawGizmos>();
            if (RoleType == RoleType.Enemy)
            {
                idRoleGo.AddComponent<Test_EnemyCmd>();
            }
#endif
        }

        protected void CreateRoleBody(string bodyName)
        {
            string bodyPath = XCPathConfig.GetRoleBodyPath(bodyName);
            body = ResMgr.LoadInstan(bodyPath,PackageType.ExtraPackage);
            body.transform.SetParent(idRole.transform, false);
            BaseInit();
        }

        protected void BaseInit()
        {
            idRole.animator = body.GetComponent<Animator>();
            idRole.animator.runtimeAnimatorController = idRole.runtimeAnim;
            idRole.animator.applyRootMotion = false;
            int settingId = RaceIdSetting.GetConfigId(raceId);
            roleData.moveSetting = ConfigMgr.LoadSoConfig<MoveSettingSo>().GetOnArray(settingId);
        }

        protected void SetTeam(int team)
        {
            this.team = team;
            var layer = GameSetting.GetTeamLayer(team);
            foreach (var item in idRole.triggerCols)
            {
                item.gameObject.layer = layer;
            }
            gameObject.layer = Layers.BODY;
        }

        public virtual void OnDamage(int atker, AtkInfo ackInfo)
        {
            //非死亡则往下执行
            if (BaseDamageCheck(ackInfo))
            {
                var setting = LubanTables.GetSkillSetting(ackInfo.skillId, ackInfo.subSkillId);
                roleData.breakData.OnHit((int)setting.BreakPower);

                if (roleData.breakData.isBreak)
                {
                    //击飞处理
                    Vector3 horDir = MathTool.RotateY(ackInfo.hitDir, setting.HorForward).normalized * setting.AddHor;

                    Vector3 targetHorVec = ackInfo.ackObjectPos + horDir * setting.AddHor;

                    float horDistance = MathTool.GetHorDistance(targetHorVec, transform.position);

                    if (!BattleData.IsTimeStop)
                    {
                        idRole.cc.DOHit(setting.AddY, horDir * horDistance, setting.HitTime);
                    }

                    transform.RotaToPos(ackInfo.hitPos, 0.5f);

                    //无重力时间
                    roleData.movement.SetNoGravityT(setting.NoGTime);

                    // 打断当前技能
                    OnBreak();
                    if (!BattleData.IsTimeStop)
                    {
                        Anim.TryPlayAnim(AnimHash.Break);
                        HitStop.Do(setting.HitStop);
                    }
                    Debug.Log($"--- AnimHash.Break");
                }
                else
                {
                    if (!BattleData.IsTimeStop)
                    {
                        Anim.TryPlayAnim(AnimHash.Hit);
                    }
                    roleData.movement.SetUnMoveTime(0.35f);
                }

                //playerMover.SetNoGravityT(setting.NoGravityT);

                var effect = RunTimePoolMgr.Inst.GetHitEffect(setting.HitEffect);
                effect.SetActive(true);
                effect.transform.SetParent(transform, true);

                Vector3 tempAckObjectPos = ackInfo.ackObjectPos;
                tempAckObjectPos.y = ackInfo.hitPos.y;
                tempAckObjectPos = Vector3.Lerp(ackInfo.ackObjectPos, ackInfo.hitPos, 0.8f);
                effect.transform.position = tempAckObjectPos; //ackInfo.hitPos;

                effect.transform.forward = ackInfo.ackObjectPos - transform.position;
            }
        }

        public virtual void OnBreak()
        {

        }

        public virtual void SetNoBusy()
        {
            roleData.roleControl.SetNoBusy();
        }

        public void CheckBreakUpdate()
        {
            roleData.breakData.OnUpdate(XCTime.deltaTime);
            if (!roleData.breakData.isBreak)
            {
                roleData.bodyState = EBodyState.Ready;
            }
        }

        // 排除异常情况:如死亡
        // 如果需要计算其他值,用ref
        private bool BaseDamageCheck(AtkInfo atkInfo)
        {
            if (IsDie)
            {
                return false;
            }
            Debug.Log($"--- atkInfo.atk {atkInfo.atk}");
            int targetHp = Math.Max(Mathf.RoundToInt(Hp - atkInfo.atk), 0);
            if (targetHp <= 0)
            {
                Hp = 0;
                OnDie();
                return false;
            }
            Hp -= atkInfo.atk;


            Vector3 textPos = transform.position;
            textPos.y = atkInfo.ackObjectPos.y;
            textPos = Vector3.Lerp(atkInfo.ackObjectPos, textPos, 0.8f);
            UIMgr.Inst.PlayDamageText(atkInfo.atk, textPos);
            return true;
        }

        public override void OnDie()
        {
            base.OnDie();
            roleData.bodyState = EBodyState.Dead;
            if (!BattleData.IsTimeStop)
            {
                Anim.SetBool(AnimHash.IsDead, true);
            }
            //
        }


        public void RoleIn()
        {
            Enable = true;
            RoleMgr.Inst.roleDic.Add(id, this);
            GameEvent.Send<int, RoleChangeType>(EventType.RoleChange.Int(), id, RoleChangeType.Add);
            GameEvent.AddEventListener<bool>(EventType.TimeSpeedStop.Int(),StopTimeSpeed);
        }

        public void RoleOut()
        {
            Debug.Log($"--- RoleOut {id}");
            GameEvent.Send<int, RoleChangeType>(EventType.RoleChange.Int(), id, RoleChangeType.Remove);
            GameEvent.RemoveEventListener<bool>(EventType.TimeSpeedStop.Int(), StopTimeSpeed);
            RoleMgr.Inst.roleDic.Remove(id);
        }

        private void StopTimeSpeed(bool isOn)
        {
            if (IsPlayer)
            {
                return;
            }
            //处理击飞
            if (!isOn)
            {
                if (roleData.breakData.isBreak)
                {
                    Anim.TryPlayAnim(AnimHash.Break);
                }
                else if (roleData.breakData.HasHit)
                {
                    Anim.TryPlayAnim(AnimHash.Hit);
                }

                if (roleData.bodyState == EBodyState.Dead)
                {
                    Anim.SetBool(AnimHash.IsDead,true);
                }

            }

            roleData.roleControl.StopTimeSpeed(isOn);
        }


        public override void ReceiveMsg(EntityMsgType type, int fromId, object msg)
        {
            Debuger.Log($"--- Receive {type} fromId: {fromId}");
            switch (type)
            {
                case EntityMsgType.PlayNextSkill:
                    int skillId = (int)msg;
                    roleData.roleControl.TryPlaySkill(skillId);
                    break;
                case EntityMsgType.SetNoBusy:
                    break;
                case EntityMsgType.SetUnMoveTime:
                    SetUnMoveTime(msg);
                    break;
                case EntityMsgType.AddTag:
                    AddTag(msg);
                    break;
                case EntityMsgType.SetNoGravityTime:
                    SetNoGravityTime(msg);
                    break;
                case EntityMsgType.AutoDirect:
                    AutoDirect(msg);
                    break;                
                case EntityMsgType.NoBodyCollision:
                    NoBodyCollision(msg);
                    break;
                default:
                    break;
            }
        }
        private void NoBodyCollision(object msg)
        {
            BaseMsg baseMsg = (BaseMsg)msg;
            if (baseMsg.state == 0)
            {
                gameObject.layer = Layers.WITHOUT_BODY;
            }
            else
            {
                gameObject.layer = Layers.BODY;
            }
        }

        private void AutoDirect(object msg)
        {
            roleData.roleControl.DefaultAutoDirect();
        }
        private void SetUnMoveTime(object msg)
        {
            float t = ((BaseMsg)msg).numMsg;
            roleData.movement.SetUnMoveTime(t);
        }

        private void AddTag(object msg)
        {
            BaseMsg baseMsg = (BaseMsg)msg;
            if (baseMsg.state == 0)
            {
                base.AddTag((int)baseMsg.numMsg);
            }
            else
            {
                RemoveTag((int)baseMsg.numMsg);
            }
        }

        private void SetNoGravityTime(object msg)
        {
            BaseMsg baseMsg =( BaseMsg)msg;
            float t1 = baseMsg.numMsg;
            if (baseMsg.state == 0)
            {
                roleData.movement.SetSkillNoGravityT(t1);
            }
            else
            {
                roleData.movement.SetSkillNoGravityT(0);
            }

        }

        public virtual void AIMoveDir(Vector3 dir, float speedFactor, bool isLookDir = false)
        {
            roleData.movement.SetMoveDir(dir, speedFactor, isLookDir);
        }

        public virtual void AIMoveTo(Vector3 pos, float speedFactor, bool isLookDir = false)
        {
            var dir = (pos - gameObject.transform.position).normalized;
            roleData.movement.SetMoveDir(dir, speedFactor, isLookDir);
        }

        public virtual void AIMsg(ActMsgType actType, string actMsg)
        {
            if (actType == ActMsgType.Skill)
            {

            }
        }

        public GameObject WeaponObject { get; set; }

        public void TakeWeapon(GameObject weapon)
        {
            var hand = WeaponHelper.FindHand(Anim);
            weapon.transform.SetParent(hand.transform, false);
            weapon.transform.localPosition = Vector3.zero;
            WeaponObject = weapon;
            //TODO WeaponData weapon的属性加成
            //可以简单通过名字确定武器id, 比如weapon_1
        }
        public void RemoveWeapon()
        {
            var weapon = WeaponObject;
            Vector3 pos = idRole.transform.position;
            var camForword = CameraMgr.Forword;
            camForword.y = 0;
            Vector3 dropPos = pos + camForword.normalized * 2;
            weapon.transform.position = pos;
            WeaponObject = null;
        }
    }

    public class PlayerBase : Role
    {
        public override RoleType RoleType => RoleType.Player;

        public override void DataCreat()
        {
            throw new NotImplementedException();
        }
    }

    public class EnemyBase : Role
    {
        public override RoleType RoleType => RoleType.Enemy;

        public override void DataCreat()
        {
            throw new NotImplementedException();
        }
    }

    #region Component
    public class RoleComponent<T> : EntityComponent where T : Role
    {
        public T owner;

        public RoleComponent(T _owner)
        {
            this.owner = _owner;
        }

        public RoleData Data_R => owner.roleData;
        public RoleState RoleState => Data_R.roleState;
    }




    public class EntityComponent
    {
        /// <summary>
        /// no base
        /// </summary>
        public virtual void Update() { }
        /// <summary>
        /// no base
        /// </summary>
        public virtual void FixedUpdate() { }

    }
    #endregion

    #region Datas
    public class RoleData
    {

        public int curSkillId;

        public EBodyState bodyState;

        //public ESkillState skillState;

        public DataListener<ESkillState> skillState = new DataListener<ESkillState>();

        public BreakData breakData = new BreakData();

        public PlayerAttr playerAttr = new PlayerAttr();

        public RoleState roleState = new RoleState();

        public IRoleControl roleControl;

        //特殊,需要手动赋值
        public MoveSetting moveSetting;

        public RoleMovement movement;
        public bool IsStateFree => bodyState is not EBodyState.Break or EBodyState.Dead;

        public bool IsBusy => roleControl.IsBusy();

        //优先级规则
        //0. 非死亡&非控制中 属于自由状态
        //1. 自由状态可以执行任何主动行为
        //2. 如果在自由期间 , 被击中,可能转至被不自由状态
        //3. 处于不自由时,可以使用特殊技能
        //4. 施法优先级高于普通, 普攻无法打断施法, 但施法可打断普攻 
        //5. 处于task过程中 无法普攻
        //6. 翻滚优先级高, 可以打断普攻 和 技能
    }

    public class RoleState : IUsed
    {
        public float moveLockTime; //普通锁定移动, 如技能状态
        public float rotateLockTime; //普通锁定旋转, 如技能状态
        public float breakTime;
        public bool moveLockFlag; //强制锁定移动, 如角色死亡

        public float moveSpeedMult = 1;

        public float moveAnimMult = 1;

        public float animMoveSpeed = 0;

        public Vector3 inputDir = Vector3.zero;// 暂无用处
        public float MoveMultFinal => moveSpeedMult * moveAnimMult;
        public bool IsMoveLock => moveLockFlag || moveLockTime > 0;



        public void Used()
        {
            inputDir = Vector3.zero;
        }

    }

    public class BreakData
    {
        public float armor = 5;  //虽说有小数, 实际用整数
        public float maxArmor = 5;
        public float recoverWait_t = 0.8f;  //进入破防后,多久启动恢复
        public float recoverFinish_t = 0.5f; //恢复满需要时间
        public float recoverSpeedInner = 0; //被动恢复,没陷入破防时的恢复速度,boss用,小怪一般为0 
        public float maxBreakTime = 0;  //最大连续受击时间,默认0为无
        //死亡处理
        public float deadTimer = 0;
        public float deadTime = 1.5f;//结束时回收

        public float recoverSpeed => maxArmor / recoverFinish_t; //每秒回复多少

        public BreakSubState state { get; set; }
        public bool isHover { get; set; }//是否滞空
        public bool isBreak => state != BreakSubState.None;  //是否破防

        public bool HasHit { get; set; }

        public float ShowPercentage => armor / maxArmor;

        public float enterBreakTime;

        public float breakTimer;

        public bool UpdateDeadEnd()
        {
            deadTimer += XCTime.deltaTime;
            if (deadTimer > deadTime)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void OnHit(int hitArmor)
        {
            armor -= hitArmor;
            if (state == BreakSubState.None)
            {
                if (armor <= 0)
                {
                    state = BreakSubState.BreakStart;
                }
            }
            HasHit = true;
        }

        public void OnUpdate(float deltaTime)
        {
            if (recoverSpeedInner > 0)
            {
                armor += deltaTime * recoverSpeedInner;
            }

            if (state == BreakSubState.BreakRecover)
            {
                armor += deltaTime * recoverSpeed;
            }
            else if (state == BreakSubState.BreakStart)
            {
                breakTimer += deltaTime;
                if (breakTimer > recoverWait_t)
                {
                    state = BreakSubState.BreakRecover;
                }
            }

            if (armor >= maxArmor)
            {
                armor = maxArmor;
                state = BreakSubState.None;
            }
            HasHit = false;
        }

        public enum BreakSubState
        {
            None,
            BreakStart,
            BreakRecover
        }
    }
    #endregion

    #region Flag & interface
    public static class RoleTagCommon
    {
        public const int NoHpBar = 0;
        public const int MainPlayer = 1;
        public const int Boss = 2;
    }
    public enum EBodyState
    {
        Ready,
        Break, //被打断的中,眩晕中
        Dead  //最高优先级
    }

    public enum ESkillState
    {
        Idle,
        Skill,
        SkillEnd //后摇
    }

    //行为类型
    public enum EActType
    {
        None,
        Jump,
        NorAck,
        Skill,
        Roll,
        Other
    }
    /// <summary>
    /// 清除缓存数据, 如输入按键
    /// </summary>
    public interface IUsed
    {
        void Used();
    }

    public interface IUpdate
    {
        public void Update();

        public void FixedUpdate();
    }
    #endregion

}