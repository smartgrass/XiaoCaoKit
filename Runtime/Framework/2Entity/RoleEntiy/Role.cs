using cfg;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        public bool IsPlayer
        {
            get => RoleType == RoleType.Player;
        }

        public bool IsLocalMainPlayer
        {
            get => HasTag(RoleTagCommon.MainPlayer);
        }

        public RoleIdentityType RoleIdentityType { get; set; }

        public bool IsEnemyIdentity => RoleIdentityType == RoleIdentityType.Enemy;

        //roleData
        public RoleData data_R;

        public PlayerAttr PlayerAttr => data_R.playerAttr;

        public Action<Role> DeadAct;

        public Action<AtkInfo> AtkDamageAct;

        public int Level
        {
            get => PlayerAttr.lv;
        }

        public override int Hp
        {
            get => (int)Math.Round(PlayerAttr.hp);
            set => PlayerAttr.hp = value;
        }

        public override float MaxHp
        {
            get => PlayerAttr.MaxHp;
        }


        public float ShowArmorPercentage => data_R.breakData.ShowPercentage;

        [ShowNativeProperty] public override bool IsDie => data_R.bodyState == EBodyState.Dead;
        public bool IsFree => data_R.IsStateFree;

        public bool IsHiding = false; //隐身

        public bool IsNoDamage => PlayerAttr.GetAttribute(EAttr.NoDamage).CurrentValue > 0;

        public bool IsAnimBreak => Anim.GetCurrentAnimatorStateInfo(0).IsTag("Break");

        ///职业,种族: 每一个角色和敌人都有对应的 raceId
        ///<see cref="RaceSetting"/>
        public int raceId;

        /// 绑定的模型文件
        public string prefabId;

        //势力,相同为友军, 1为玩家 
        ///<see cref="XCSetting"/>
        public int team;

        public GameObject body;

        public IdRole idRole;

        public Action<AtkInfo, bool> OnDamageAct;

        #region Get

        internal Animator Anim => idRole.animator;
        public bool isBodyCreated => body != null;

        public RoleMovement Movement => data_R.movement;

        #endregion

        //AI控制
        public bool IsAiOn;

        public virtual void CreateIdRole(string prefabId)
        {
            this.prefabId = prefabId;

            string baseRole = XCPathConfig.GetIdRolePath(prefabId);
            GameObject idRoleGo = ResMgr.LoadInstan(baseRole, PackageType.DefaultPackage);


            idRole = idRoleGo.transform.GetComponent<IdRole>();
            idRole.id = id;
            raceId = idRole.raceId;
            idRoleGo.tag = RoleType == RoleType.Enemy ? Tags.ENEMY : Tags.PLAYER;
            BindGameObject(idRole.gameObject);
#if UNITY_EDITOR
            var testDraw = idRoleGo.AddComponent<Test_PlayerCmd>();
            if (RoleType == RoleType.Enemy)
            {
                idRoleGo.AddComponent<Test_EnemyCmd>();
            }
#endif
        }

        protected void CreateRoleBody(string bodyName)
        {
            body = LoadModelByKey(bodyName);
            body.transform.SetParent(idRole.transform, false);
            body.transform.localPosition = Vector3.zero;
            tempRender = null;
            BaseInit();
        }

        public static GameObject LoadModelByKey(string shortKey)
        {
            string bodyPath = XCPathConfig.GetRoleBodyPath(shortKey);
            return ResMgr.TryShorKeyInst(shortKey, bodyPath);
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
            data_R.moveSetting = idRole.moveSetting;
            if (body.TryGetComponent<RoleBody>(out RoleBody roleBody))
            {
                roleBody.OnBodyCreate(this);
            }
        }

        public void SetTeam(int team)
        {
            this.team = team;
            var layer = XCSetting.GetTeamLayer(team);
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
                HitTween(ackInfo, setting, true);
                return;
            }


            SoundMgr.Inst.PlayHitAudio(setting.HitClip);
            //TODO 暴击音效
            if (ackInfo.isCrit)
            {
            }

            data_R.breakData.OnHit((int)setting.BreakPower);
            //TODO 累计血量的BreakPower加成

            data_R.movement.OnDamage(data_R.breakData.IsBreak);

            OnDamageAct?.Invoke(ackInfo, data_R.breakData.IsBreak);
            AfterDamage(ackInfo);

            if (!BattleData.IsTimeStop)
            {
                HitStop.Do(setting.HitStop);
            }

            HitTween(ackInfo, setting, data_R.breakData.IsBreak);

            if (data_R.breakData.IsBreak)
            {
                //无重力时间
                data_R.movement.SetNoGravityT(setting.NoGTime);

                // 打断当前技能
                Debug.Log($"--- OnBreak {this.transform.name} {data_R.tempCurSkillId}");
                OnBreak();
                if (!BattleData.IsTimeStop)
                {
                    transform.RotaToPos(ackInfo.hitPos, 0.5f);
                    Anim.TryPlayAnim(AnimHash.Break);
                }
            }
            else
            {
                if (!BattleData.IsTimeStop && !data_R.IsBusy)
                {
                    //boss处理下
                    if (data_R.breakData.IsHitAnimCdFinish)
                    {
                        Anim.TryPlayAnim(AnimHash.Hit);
                        data_R.breakData.lastHitAnimTime = Time.time;
                    }
                }

                data_R.movement.SetUnMoveTime(0.35f);
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
            data_R.playerAttr.ChangeNowValue(ENowAttr.Hp, delta);
        }

        //击退处理
        private void HitTween(AtkInfo ackInfo, SkillSetting setting, bool isBreak)
        {
            float deltaXZ = MathTool.GetHorDistance(ackInfo.ackObjectPos, transform.position);
            float horMoveDistance = GetBalanceValue(setting.AddHor, deltaXZ);
            Vector3 horMoveVec = MathTool.RotateY(ackInfo.hitDir, setting.HorForward).normalized * horMoveDistance;

            float deltaY = ackInfo.ackObjectPos.y - transform.position.y;
            float addY = GetBalanceValue(setting.AddY, deltaY);
            if (!isBreak)
            {
                addY = 0;
            }

            if (!BattleData.IsTimeStop)
            {
                float w = idRole.moveSetting.weight;
                idRole.cc.DOHit(addY / w, horMoveVec / w, setting.HitTime / w);
            }
        }

        //利用除法公式, 计算平衡数值
        private float GetBalanceValue(float target, float delta)
        {
            return target * target / (delta + target);
        }


        public virtual void OnBreak()
        {
        }


        public void CheckBreakUpdate()
        {
            data_R.breakData.OnUpdate(XCTime.deltaTime);
            if (!data_R.breakData.IsBreak)
            {
                data_R.bodyState = EBodyState.Ready;
            }
        }

        private int tempHpProcces;

        // 排除异常情况:如死亡
        // 如果需要计算其他值,用ref
        private bool BaseDamageCheck(AtkInfo atkInfo)
        {
            if (IsDie)
            {
                return false;
            }

            float hurtProcess = atkInfo.atk / MaxHp;
            if (Hp / MaxHp > tempHpProcces)
            {
                //小兵为10 boss可能为30
                BattleData.Current.AddFightVale(hurtProcess * 10);
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

            GameEvent.Send<int, bool, AtkInfo>(EGameEvent.RoleHurt.ToInt(), id, IsPlayer, atkInfo);

            return true;
        }

        public override void OnDie(AtkInfo atkInfo)
        {
            Hp = 0;
            base.OnDie(atkInfo);
            data_R.roleControl.BreakAllBusy();
            data_R.bodyState = EBodyState.Dead;
            if (!BattleData.IsTimeStop)
            {
                Anim.SetBool(AnimHash.IsDead, true);
            }

            DeadAct?.Invoke(this);

            if (RoleIdentityType == RoleIdentityType.PlayerFriend)
            {
                XCTime.DelayRunMono(5, OnReborn, idRole);
            }

            if (IsLocalMainPlayer)
            {
                GameEvent.Send<int>(EGameEvent.PlayerDead.ToInt(), id);
            }
            
            gameObject.layer = Layers.WITHOUT_BODY;
        }

        //复活
        public void OnReborn()
        {
            Hp = (int)MaxHp;

            data_R.bodyState = EBodyState.Ready;

            gameObject.layer = Layers.BODY_PHYSICS;

            Anim.SetBool(AnimHash.IsDead, false);
        }

        public void InitRoleData()
        {
            var setting = ConfigMgr.Inst.AttrSettingSo;
            int lv = data_R.playerAttr.lv;
            int attrSettingId = 0;
            if (!IsPlayer)
            {
                AddEnemyData aiData = idRole.gameObject.GetComponent<AddEnemyData>();
                attrSettingId = aiData.attSettingId;
            }

            if (!setting.ContainsKey(attrSettingId))
            {
                Debug.LogError($"--- attrSettingId no {attrSettingId}");
            }

            AttrSetting attr = setting.GetOrDefault(attrSettingId, 0);

            data_R.breakData.SetAttr(attr, this);

            data_R.playerAttr.Init(id, lv, attr);
        }

        public void RoleIn()
        {
            Enable = true;
            RoleMgr.Inst.roleDic.Add(id, this);
            GameEvent.Send<int, RoleChangeType>(EGameEvent.RoleChange.ToInt(), id, RoleChangeType.Add);
            GameEvent.AddEventListener<bool>(EGameEvent.TimeSpeedStop.ToInt(), StopTimeSpeed);
        }

        public void RoleOut()
        {
            Debug.Log($"--- RoleOut {id}");
            GameEvent.Send<int, RoleChangeType>(EGameEvent.RoleChange.ToInt(), id, RoleChangeType.Remove);
            GameEvent.RemoveEventListener<bool>(EGameEvent.TimeSpeedStop.ToInt(), StopTimeSpeed);
            RoleMgr.Inst.roleDic.Remove(id);
        }

        public virtual void OnAtkDamage(AtkInfo info)
        {
            float AtkRecoverHp = PlayerAttr.AtkRecoverHp;
            if (AtkRecoverHp > 0)
            {
                float recover = info.atk * AtkRecoverHp;
                PlayerAttr.ChangeNowValue(ENowAttr.Hp, recover);
            }

            AtkDamageAct?.Invoke(info);
        }

        private void StopTimeSpeed(bool isOn)
        {
            if (!IsEnemyIdentity)
            {
                return;
            }

            //处理击飞
            if (!isOn)
            {
                if (data_R.breakData.HasHit)
                {
                    if (data_R.breakData.IsBreak)
                    {
                        Anim.TryPlayAnim(AnimHash.Break);
                    }
                    else
                    {
                        Anim.TryPlayAnim(AnimHash.Hit);
                    }
                }

                if (data_R.bodyState == EBodyState.Dead)
                {
                    Anim.SetBool(AnimHash.IsDead, true);
                }
            }

            data_R.roleControl.StopTimeSpeed(isOn);
        }

        ///<see cref="BaseMsg"/>
        public override void ReceiveMsg(EntityMsgType type, int fromId, object msg)
        {
            Debuger.Log($"--- Receive {type} fromId: {fromId}");
            switch (type)
            {
                case EntityMsgType.PlayNextSkill:
                    PlayNextSkill(msg);
                    break;
                case EntityMsgType.SetNoBusy:
                    //无需处理
                    break;
                case EntityMsgType.SetUnMoveTime:
                    SetUnMoveTime(msg);
                    break;
                case EntityMsgType.SetUnRotate:
                    SetUnRotateTime(msg);
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
                case EntityMsgType.TheWorld:
                    SetTimeStop(msg);

                    break;
                case EntityMsgType.NoBreakTime:
                    OnNoBreakTime(msg);
                    break;
                default:
                    break;
            }
        }

        private void PlayNextSkill(object msg)
        {
            string skillId = ((BaseMsg)msg).strMsg;
            if (data_R.roleControl.IsBusy())
            {
                data_R.roleControl.BreakAllBusy();
            }

            data_R.roleControl.TryPlaySkill(skillId);
        }

        private void OnNoBreakTime(object msg)
        {
            BaseMsg baseMsg = (BaseMsg)msg;
            float time = baseMsg.numMsg;
            if (baseMsg.state == 0)
            {
                data_R.breakData.SetNoBreakTime(time);
            }
            else
            {
                data_R.breakData.SetNoBreakTime(0, false);
            }
        }

        private void SetTimeStop(object msg)
        {
            BaseMsg baseMsg = (BaseMsg)msg;
            float time = baseMsg.numMsg;
            TimeStopMgr.Inst.StopTimeSpeed(time);
        }

        private void OnNoDamage(object msg)
        {
            BaseMsg baseMsg = (BaseMsg)msg;
            if (baseMsg.state == 0)
            {
                AttributeModifier modifier = new AttributeModifier { Add = 1 };
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
            data_R.roleControl.DefaultAutoDirect();
        }

        private void SetUnMoveTime(object msg)
        {
            float t = ((BaseMsg)msg).numMsg;
            data_R.movement.SetUnMoveTime(t);
        }

        private void SetUnRotateTime(object msg)
        {
            float t = ((BaseMsg)msg).numMsg;
            data_R.movement.SetUnRotateTime(t);
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
                data_R.movement.SetSkillNoGravityT(t1);
            }
            else
            {
                data_R.movement.SetSkillNoGravityT(0);
            }
        }

        /// <summary>
        /// 移动
        /// </summary>
        /// <param name="vector">长度有效</param>
        /// <param name="animSpeedFactor">影响移动动画的最大值</param>
        /// <param name="isLookDir">是否根据移动方向旋转</param>
        public virtual void AIMoveVector(Vector3 vector, float animSpeedFactor, bool isLookDir = false)
        {
            data_R.movement.SetMoveDir(vector, animSpeedFactor, isLookDir);
        }

        /// <summary>
        /// 移动到指定位置
        public virtual void AIMoveTo(Vector3 pos, float moveSpeed, float animSpeedFactor, bool isLookDir = false)
        {
            var dir = (pos - gameObject.transform.position).normalized * moveSpeed;
            data_R.movement.SetMoveDir(dir, animSpeedFactor, isLookDir);
        }

        public void AISetLookTarget(Transform target)
        {
            data_R.movement.SetLookTarget(target);
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

        public RoleData Data_R => owner.data_R;
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
        public virtual void Update()
        {
        }

        /// <summary>
        /// no base
        /// </summary>
        public virtual void FixedUpdate()
        {
        }
    }

    #endregion

    #region Datas

    public class RoleData
    {
        public string tempCurSkillId = "";

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

        public bool IsStateFree => bodyState is not (EBodyState.Break or EBodyState.Dead);

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

        public float animMoveSpeed = 0; //最大为1 maxAnimMoveSpeed

        public Vector3 inputDir = Vector3.zero; // 暂无用处
        public bool IsMoveLock => moveLockFlag || moveLockTime > 0;


        public void Used()
        {
            inputDir = Vector3.zero;
        }
    }

    public enum BreakCdState
    {
        None,
        Break,
    }

    public class BreakData
    {
        public float maxArmor = 4;

        public float recoverCdOnBreak = 4; //进入Break后多久触发恢复/眩晕时间

        public float recoverCdIfOnHurt = 4; //角色未受伤后多久触发恢复

        public float actionRecover = 0.1f; //攻击时恢复

        public float noHurtRecoverSpeed = 0.2f; //不受击时恢复速度

        public bool isBoss;

        //死亡处理
        public float deadTimer = 0;
        public float deadTime = 3f; //结束时回收

        #region runtimeData

        public float armor = 4; //虽说有小数, 实际用整数

        public float armorDef = 0;

        private float _recoverCdTimer = 0;

        private float _lastHitTime;

        private float _noBreakTimer;

        public bool HasHit { get; set; }

        public bool isHover { get; set; } //是否滞空
        public bool IsBreak => armor <= 0;
        public float ShowPercentage => armor / maxArmor;

        public float lastHitAnimTime;
        public bool IsHitAnimCdFinish => Time.time - lastHitAnimTime > hitAnimSpan;

        public float hitAnimSpan = 0.5f;

        private BreakCdState _state;

        public BreakCdState SetState
        {
            set
            {
                if (_state != value)
                {
                    OnBreakStateChange(_state, value);
                }

                _state = value;
            }
        }

        #endregion

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
            if (_noBreakTimer > 0)
            {
                return;
            }

            armor -= Mathf.Max(0, hitArmor - armorDef);
            _lastHitTime = Time.time;
            HasHit = true;

            if (armor <= 0)
            {
                SetState = BreakCdState.Break;
            }
        }

        public void OnUpdate(float deltaTime)
        {
            if (_state == BreakCdState.Break)
            {
                _recoverCdTimer -= deltaTime;
                if (_recoverCdTimer <= 0)
                {
                    armor = maxArmor;
                    SetState = BreakCdState.None;
                }
            }

            if (_lastHitTime + recoverCdIfOnHurt < Time.time)
            {
                armor += deltaTime * noHurtRecoverSpeed * maxArmor;

                if (armor >= maxArmor)
                {
                    armor = maxArmor;
                }
            }


            if (_noBreakTimer > 0)
            {
                _noBreakTimer -= deltaTime;
            }

            HasHit = false;
        }


        private void OnBreakStateChange(BreakCdState old, BreakCdState newState)
        {
            if (newState == BreakCdState.Break)
            {
                _recoverCdTimer = recoverCdOnBreak;
            }
        }

        internal void SetAttr(AttrSetting attr, Role role)
        {
            maxArmor = attr.maxArmor;
            armor = attr.maxArmor;
            armorDef = attr.armorDef;
            recoverCdOnBreak = attr.recoverCdOnBreak;
            recoverCdIfOnHurt = attr.recoverCdIfOnHurt;
            actionRecover = attr.actionRecover;
            noHurtRecoverSpeed = attr.noHurtRecoverSpeed;
            deadTime = role.data_R.moveSetting.deadTime;
            isBoss = attr.IsBoss;
            role.AddTag(RoleTagCommon.Boss);
            hitAnimSpan = isBoss ? 0.5f : 0.1f;
        }

        public void SetNoBreakTime(float time, bool isForce = false)
        {
            if (_noBreakTimer >= time && !isForce)
            {
                return;
            }

            _noBreakTimer = time;
            if (armor <= 0)
            {
                armor = 0.1f * maxArmor;
            }
        }
    }

    #endregion

    #region Flag & interface

    public static class RoleTagCommon
    {
        public const int NoHpBar = 0;
        public const int MainPlayer = 1;
        public const int Boss = 2;
        public const int EnableAiIfHurt = 100;
    }

    public enum EBodyState
    {
        Ready,
        Break, //被打断的中,眩晕中
        Dead //最高优先级
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
        Level,
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