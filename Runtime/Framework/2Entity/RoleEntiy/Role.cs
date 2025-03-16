
using cfg;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using XiaoCao.Render;
using Debug = UnityEngine.Debug;
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

        public bool IsPlayer { get => RoleType == RoleType.Player; }

        //roleData
        public RoleData roleData;

        public PlayerAttr PlayerAttr => roleData.playerAttr;

        public Action<Role> DeadAct;

        public Action<AtkInfo> AtkDamageAct;

        public int Level { get => PlayerAttr.lv; set => PlayerAttr.lv = value; }
        public override int Hp { get => (int)Math.Round(PlayerAttr.hp); set => PlayerAttr.hp = value; }
        public override float MaxHp { get => PlayerAttr.MapHp; }

        public float ShowArmorPercentage => roleData.breakData.ShowPercentage;

        [ShowNativeProperty]
        public override bool IsDie => roleData.bodyState == EBodyState.Dead;
        public bool IsFree => roleData.IsStateFree;

        public bool IsNoDamage => PlayerAttr.GetAttribute(EAttr.NoDamage).CurrentValue > 0;

        public bool IsAnimBreak => Anim.GetCurrentAnimatorStateInfo(0).IsTag("Break");

        ///职业,种族: 每一个角色和敌人都有对应的 raceId
        ///<see cref="RaceSetting"/>
        public int raceId;

        /// 绑定的模型文件
        public string prefabId;

        //势力,相同为友军
        public int team;

        public GameObject body;

        public IdRole idRole;

        public Action<AtkInfo, bool> OnDamageAct;

        #region Get
        internal Animator Anim => idRole.animator;
        public bool isBodyCreated => body != null;

        public RoleMovement Movement => roleData.movement;

        #endregion

        //AI控制
        public bool IsAiOn;

        public virtual void CreateIdRole(string prefabId)
        {
            this.prefabId = prefabId;

            string baseRole = XCPathConfig.GetIdRolePath(RoleType, prefabId);
            GameObject idRoleGo = ResMgr.LoadInstan(baseRole, PackageType.DefaultPackage);


            idRole = idRoleGo.transform.GetComponent<IdRole>();
            idRole.id = id;
            raceId = idRole.raceId;
            idRoleGo.tag = RoleType == RoleType.Enemy ? Tags.ENEMY : Tags.PLAYER;
            BindGameObject(idRole.gameObject);
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
            string shortKey = bodyName;
            string bodyPath = XCPathConfig.GetRoleBodyPath(bodyName, RoleType);
            body = ResMgr.TryShorKeyInst(shortKey, bodyPath);
            body.transform.SetParent(idRole.transform, false);
            BaseInit();
        }

        public void ChangeBody(string bodyName)
        {
            GameObject.Destroy(body);
            Debug.Log($"--- ChangeBody bodyName");
            CreateRoleBody(bodyName);

        }

        protected void BaseInit()
        {
            idRole.animator = body.GetComponent<Animator>();
            idRole.animator.runtimeAnimatorController = idRole.LoadRuntimeAc;
            idRole.animator.applyRootMotion = false;
            roleData.moveSetting = idRole.moveSetting;
        }

        protected void SetTeam(int team)
        {
            this.team = team;
            var layer = GameSetting.GetTeamLayer(team);
            foreach (var item in idRole.triggerCols)
            {
                item.gameObject.layer = layer;
            }
            gameObject.layer = Layers.BODY_PHYSICS;
        }

        public virtual void OnDamage(AtkInfo ackInfo)
        {
            if (IsDie)
            {
                return;
            }

            if (IsNoDamage)
            {
                return;
            }

            var setting = ackInfo.GetSkillSetting;

            if (ackInfo.IsLocalPlayer)
            {
                CamEffectMgr.Inst.CamShakeEffect(setting.ShakeLevel);
            }

            //非死亡则往下执行
            if (!BaseDamageCheck(ackInfo))
            {
                SoundMgr.Inst.PlayHitAudio("Dead");
                HitTween(ackInfo, setting);
                return;
            }

            SoundMgr.Inst.PlayHitAudio(setting.HitClip);

            roleData.breakData.OnHit((int)setting.BreakPower);
            roleData.movement.OnDamage(roleData.breakData.IsBreak);

            OnDamageAct?.Invoke(ackInfo, roleData.breakData.IsBreak);
            AfterDamage(ackInfo);

            if (roleData.breakData.IsBreak)
            {
                HitTween(ackInfo, setting);

                //无重力时间
                roleData.movement.SetNoGravityT(setting.NoGTime);

                // 打断当前技能
                OnBreak();
                if (!BattleData.IsTimeStop)
                {
                    transform.RotaToPos(ackInfo.hitPos, 0.5f);
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
            HitHelper.ShowHitEffect(transform, ackInfo);
        }

        public void AfterDamage(AtkInfo ackInfo)
        {
            ackInfo.beAtker = id;
            var role = ackInfo.atker.GetRoleById();
            if (role != null)
            {
                role.OnAtkDamage(ackInfo);
            }
        }

        public void ChangeNowValue(ENowAttr eAttr, float delta)
        {
            roleData.playerAttr.ChangeNowValue(ENowAttr.Hp, delta);
        }

        private void HitTween(AtkInfo ackInfo, SkillSetting setting)
        {
            //击飞处理
            Vector3 horDir = MathTool.RotateY(ackInfo.hitDir, setting.HorForward).normalized * setting.AddHor;

            Vector3 targetHorVec = ackInfo.ackObjectPos + horDir * setting.AddHor;

            float horDistance = MathTool.GetHorDistance(targetHorVec, transform.position);

            float deltaY = ackInfo.ackObjectPos.y - transform.position.y;

            float addY = setting.AddY;

            //限制高
            if (deltaY < 0)
            {
                addY = setting.AddY + deltaY;
            }

            if (!BattleData.IsTimeStop)
            {
                float w = idRole.weight;
                idRole.cc.DOHit(addY / w, horDir * horDistance / w, setting.HitTime / w);
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
            if (!roleData.breakData.IsBreak)
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

                OnDie(atkInfo);
                return false;
            }
            Hp -= atkInfo.atk;

            HitHelper.ShowDamageText(transform, atkInfo.atk, atkInfo);
            return true;
        }

        public override void OnDie(AtkInfo atkInfo)
        {
            base.OnDie(atkInfo);
            roleData.bodyState = EBodyState.Dead;
            if (!BattleData.IsTimeStop)
            {
                Anim.SetBool(AnimHash.IsDead, true);
            }
            DeadAct?.Invoke(this);
        }

        public void InitRoleData()
        {
            var setting = ConfigMgr.commonSettingSo;
            int lv = roleData.playerAttr.lv;
            AttrSetting attr = IsPlayer ? setting.playerSetting : setting.enemySetting;
            roleData.breakData.maxArmor = attr.maxArmor;
            roleData.playerAttr.Init(id, lv, attr);
        }

        public void RoleIn()
        {
            Enable = true;
            RoleMgr.Inst.roleDic.Add(id, this);
            GameEvent.Send<int, RoleChangeType>(EGameEvent.RoleChange.Int(), id, RoleChangeType.Add);
            GameEvent.AddEventListener<bool>(EGameEvent.TimeSpeedStop.Int(), StopTimeSpeed);
        }

        public void RoleOut()
        {
            Debug.Log($"--- RoleOut {id}");
            GameEvent.Send<int, RoleChangeType>(EGameEvent.RoleChange.Int(), id, RoleChangeType.Remove);
            GameEvent.RemoveEventListener<bool>(EGameEvent.TimeSpeedStop.Int(), StopTimeSpeed);
            RoleMgr.Inst.roleDic.Remove(id);
        }

        public virtual void OnAtkDamage(AtkInfo info)
        {
            float AtkRecoverHp = PlayerAttr.GetValue(EAttr.AtkRecoverHp);
            if (AtkRecoverHp > 0)
            {
                float recover = info.atk * AtkRecoverHp;
                PlayerAttr.ChangeNowValue(ENowAttr.Hp, recover);
            }
            AtkDamageAct?.Invoke(info);
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
                if (roleData.breakData.HasHit)
                {
                    if (roleData.breakData.IsBreak)
                    {
                        Anim.TryPlayAnim(AnimHash.Break);
                    }
                    else
                    {
                        Anim.TryPlayAnim(AnimHash.Hit);
                    }
                }

                if (roleData.bodyState == EBodyState.Dead)
                {
                    Anim.SetBool(AnimHash.IsDead, true);
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
                    string skillId = (string)msg;
                    roleData.roleControl.TryPlaySkill(skillId);
                    break;
                case EntityMsgType.SetNoBusy:
                    SetNoBusy();
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
                case EntityMsgType.CameraShake:
                    OnCameraShake(msg);
                    break;
                case EntityMsgType.BodyPhantom:
                    OnBodyPhantom(msg);
                    break;
                case EntityMsgType.HideRender:
                    OnHideRender(msg);
                    break;
                case EntityMsgType.AnimSpeed:
                    OnAnimSpeed(msg);
                    break;                
                case EntityMsgType.NoDamage:
                    OnNoDamage(msg);
                    break;
                default:
                    break;
            }
        }

        private void OnNoDamage(object msg)
        {
            BaseMsg baseMsg = (BaseMsg)msg;
            if (baseMsg.state == 0)
            {
                AttributeModifier modifier = new AttributeModifier{Add = 1};
                PlayerAttr.ChangeAttrValue(EAttr.NoDamage, baseMsg.strMsg, modifier);
            }
            else
            {
                PlayerAttr.RemoveModifier(EAttr.NoDamage, baseMsg.strMsg);
            }
        }

        private void OnAnimSpeed(object msg)
        {
            BaseMsg baseMsg = (BaseMsg)msg;
            if (baseMsg.state == 0)
            {
                Anim.speed = baseMsg.numMsg;
            }
            else
            {
                //TODO 完善
                Anim.speed = XCTime.timeScale;
            }
        }

        private Renderer[] tempRender;

        private void OnHideRender(object msg)
        {
            BaseMsg baseMsg = (BaseMsg)msg;
            if (tempRender == null)
            {
                tempRender = body.GetComponentsInChildren<Renderer>();
            }

            if (baseMsg.state == 0)
            {
                //body.gameObject.SetActive(false);
                foreach (Renderer renderer in tempRender)
                {
                    renderer.enabled = false;
                }
            }
            else
            {
                foreach (Renderer renderer in tempRender)
                {
                    renderer.enabled = true;
                }
                //body.gameObject.SetActive(true);
            }
        }
        private void OnBodyPhantom(object msg)
        {
            BaseMsg baseMsg = (BaseMsg)msg;
            var p = body.GetOrAddComponent<BodyPhantom>();
            if (baseMsg.state == 0)
            {
                p.StartAnim(baseMsg.numMsg);
            }
            else
            {
                p.StopAnim();
            }

        }

        private void OnCameraShake(object msg)
        {
            BaseMsg baseMsg = (BaseMsg)msg;
            if (this.id.IsLocalPlayerId())
            {
                CamEffectMgr.Inst.CamShakeEffect((int)baseMsg.numMsg);
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
                gameObject.layer = Layers.BODY_PHYSICS;
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
            BaseMsg baseMsg = (BaseMsg)msg;
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

        public void AISetLookTarget(Vector3 target)
        {
            roleData.movement.SetMoveDir(target);
        }

        public virtual void AIMsg(ActMsgType actType, string actMsg)
        {
            if (actType == ActMsgType.Skill)
            {

            }
        }

        public bool FindEnemy(out Role findRole, float dis = 8, float angle = 30)
        {
            findRole = RoleMgr.Inst.SearchEnemyRole(gameObject.transform, dis, angle, out float maxScore, team);
            if (findRole != null)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public const string WeaponPointName = "WeaponPoint";
        public const string WeaponFirePointName = "WeaponFirePoint";
        public GameObject WeaponObject { get; set; }

        public Dictionary<string, Transform> pointCache = new Dictionary<string, Transform>();

        public bool GetPonitCache(string pointName, out Transform tf)
        {
            if (pointCache.TryGetValue(pointName, out tf))
            {
                return tf != null;
            }
            tf = transform.FindChildEx(pointName);
            pointCache[pointName] = tf;
            return tf != null;
        }

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
        public RoleMoveData RoleState => Data_R.roleState;

        public virtual void OnDestroy()
        {

        }
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

        public string curSkillId;

        public EBodyState bodyState;

        //public ESkillState skillState;

        public DataListener<ESkillState> skillState = new DataListener<ESkillState>();

        public BreakData breakData = new BreakData();

        public PlayerAttr playerAttr = new PlayerAttr();

        public RoleMoveData roleState = new RoleMoveData();

        public IRoleControl roleControl;

        //特殊,需要手动赋值
        public MoveSettingSo moveSetting;

        public RoleMovement movement;

        public Role lastEnemy;

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

    public class RoleMoveData : IUsed
    {
        public float moveLockTime; //普通锁定移动, 如技能状态
        public float rotateLockTime; //普通锁定旋转, 如技能状态
        public float breakTime;
        public bool moveLockFlag; //强制锁定移动, 如角色死亡

        public float moveSpeedMult = 1;

        public float moveAnimMult = 1;

        public float angleSpeedMult = 1;

        public float animMoveSpeed = 0;

        public Vector3 inputDir = Vector3.zero;// 暂无用处
        public bool IsMoveLock => moveLockFlag || moveLockTime > 0;



        public void Used()
        {
            inputDir = Vector3.zero;
        }

    }

    public class BreakData
    {
        public float armor = 4;  //虽说有小数, 实际用整数
        public float maxArmor = 4;
        public float recoverWait_t = 0.8f;  //进入破防后,多久启动恢复
        public float recoverFinish_t = 0.5f; //恢复满需要时间
        public float recoverSpeedInner = 0; //被动恢复,没陷入破防时的恢复速度,boss用,小怪一般为0 
        public float maxBreakTime = 0;  //最大连续受击时间,默认0为无
        //死亡处理
        public float deadTimer = 0;
        public float deadTime = 3f;//结束时回收

        public float recoverSpeed => maxArmor / recoverFinish_t; //每秒回复多少

        public bool isHover { get; set; }//是否滞空
        public bool IsBreak => armor <= 0;

        public bool HasHit { get; set; }

        public float ShowPercentage => armor / maxArmor;

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
        private float _lastHitTime;
        public void OnHit(int hitArmor)
        {
            armor -= hitArmor;
            _lastHitTime = Time.time;
            HasHit = true;
        }

        public void OnUpdate(float deltaTime)
        {
            if (recoverSpeedInner > 0)
            {
                armor += deltaTime * recoverSpeedInner;
            }

            if (_lastHitTime + recoverWait_t < Time.time)
            {
                armor += deltaTime * recoverSpeed;
            }


            if (armor >= maxArmor)
            {
                armor = maxArmor;
            }
            HasHit = false;
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


    public enum ENowAttr
    {
        Hp,
        Mp,
    }

    public enum EAttr
    {
        MaxHp,
        MaxMp,
        Atk,
        Crit,
        Def,
        SkillCDOff,
        AtkRecoverHp,
        MoveSpeedMult,
        //非基础属性 分界线
        NorAtkSpeedAdd,
        NoDamage, //无伤
    }

    public static class EAttrExtend
    {
        private static bool IsBaseAttr(this EAttr eAttr)
        {
            return (int)eAttr <= (int)EAttr.AtkRecoverHp;
        }
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