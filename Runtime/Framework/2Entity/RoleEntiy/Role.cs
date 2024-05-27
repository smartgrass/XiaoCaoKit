
using cfg;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using TEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UIElements;

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

        //roleData
        public RoleData roleData;

        public override int Hp { get => roleData.playerAttr.hp; set => roleData.playerAttr.hp = value; }
        public override int MaxHp { get => roleData.playerAttr.maxHp; set => roleData.playerAttr.maxHp = value; }
        [ShowNativeProperty]
        public override bool IsDie => roleData.bodyState == EBodyState.Dead;


        ///职业,种族: 每一个角色和敌人都有对应的 raceId
        ///<see cref="PlayerSetting"/>
        public int raceId;

        /// 绑定的模型文件
        public int prefabID;

        //势力,相同为友军
        public int team;

        protected int settingId;


        public GameObject body;

        public IdRole idRole;


        #region Get
        internal Animator Anim => idRole.animator;
        public bool isBodyCreated => body != null;
        #endregion

        //AI控制
        public bool IsOnAi { get; set; }

        public virtual void CreateGameObject(bool isGen = false)
        {
            GenRoleBody(prefabID);
        }

        protected void GenRoleBody(int prefabId)
        {
            string path = XCPathConfig.GetRolePrefabPath(RoleType, prefabId);

            GameObject go = ResMgr.LoadInstan(path);

            Debuger.Log($"--- path {path}");

            string baseRole = XCPathConfig.GetRoleBasePath(RoleType);
            GameObject baseGo = ResMgr.LoadInstan(baseRole, PackageType.ExtraPackage);
            baseGo.tag = Tags.PLAYER;
            idRole = baseGo.transform.GetComponent<IdRole>();
            idRole.id = id;
            raceId = idRole.raceId;
            go.transform.SetParent(baseGo.transform, false);
            body = go;
            BindGameObject(baseGo);

        }

        //一般用不上
        private void OtherGen(GameObject go)
        {
            if (go.transform.TryGetComponent<IdRole>(out IdRole getRole))
            {
                idRole = getRole;
                idRole.id = id;
                body = go.transform.Find("body").gameObject;
                BindGameObject(go);
            }
        }

        private void RoleProcess()
        {
            //加载动画机
            //刚体, 碰撞体
            //加载body, 赋予动画机


        }


        public virtual void OnDamage(int atker, AtkInfo ackInfo)
        {
            //非死亡则往下执行
            if (BaseDamageCheck(ackInfo))
            {
                var setting = LubanTables.GetSkillSetting(ackInfo.skillId, ackInfo.subSkillId);
                roleData.breakState.OnHit((int)setting.BreakPower);

                DebugGUI.Log("breakArmor", roleData.breakState.armor);

                HitStop.Do(setting.HitStop);

                if (roleData.breakState.isBreak)
                {
                    Anim.Play(AnimNames.Break);

                    //击飞处理
                    Vector3 horDir = MathTool.RotateY(ackInfo.hitDir, setting.HorForward).normalized * setting.AddHor;

                    Vector3 targetHorVec = ackInfo.ackObjectPos + horDir * setting.AddHor;

                    float horDistance = MathTool.GetHorDistance(targetHorVec, transform.position);

                    idRole.cc.DOHit(setting.AddY, horDir * horDistance, setting.HitTime);

                    transform.RotaToPos(ackInfo.hitPos, 0.5f);

                    //无重力时间
                    roleData.movement.SetNoGravityT(setting.HitTime * setting.NoGTimeMulti);

                    HitStop.Do(setting.HitStop);
                    // 打断当前技能
                    OnBreak();
                }

                //playerMover.SetNoGravityT(setting.NoGravityT);

                var effect = RunTimePoolMgr.Inst.GetHitEffect(setting.HitEffect);
                effect.SetActive(true);
                effect.transform.SetParent(transform,true);
                //Vector3 vector3 = transform.position;
                //vector3.y = ackInfo.hitPos.y;
                //vector3 = Vector3.Lerp(ackInfo.ackObjectPos, vector3, 0.8f);
                effect.transform.position = ackInfo.hitPos;
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
            roleData.breakState.OnUpdate(XCTime.deltaTime);
            if (!roleData.breakState.isBreak)
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
            int targetHp = Math.Max(Mathf.RoundToInt(Hp - atkInfo.atk), 0);
            if (targetHp <= 0)
            {
                OnDie();
                return false;
            }
            Hp -= atkInfo.atk;
            return true;
        }

        public override void OnDie()
        {
            base.OnDie();
            roleData.bodyState = EBodyState.Dead;
            Anim.Play(AnimNames.Dead);
        }

        protected void BaseInit()
        {
            idRole.animator = body.GetComponent<Animator>();
            idRole.animator.runtimeAnimatorController = idRole.runtimeAnim;

            settingId = RaceIdSetting.GetConfigId(raceId);
            roleData.moveSetting = ConfigMgr.LoadSoConfig<MoveSettingSo>().GetSetting(settingId);

            gameObject.layer = GameSetting.GetTeamLayer(team);


        }

        public void RoleIn()
        {
            Enable = true;
            RoleMgr.Inst.roleDic.Add(id, this);
            GameEvent.Send<int, RoleChangeType>(EventType.RoleChange.Int(), id, RoleChangeType.Add);
        }

        public void RoleOut()
        {
            GameEvent.Send<int, RoleChangeType>(EventType.RoleChange.Int(), id, RoleChangeType.Remove);
            RoleMgr.Inst.roleDic.Remove(id);
        }

        public override void ReceiveMsg(EntityMsgType type, int fromId, object msg)
        {
            Debug.Log($"--- Receive {type} fromId: {fromId}");
            if (type is EntityMsgType.SetUnMoveTime)
            {
                float t = ((BaseMsg)msg).numMsg;
                roleData.movement.SetUnMoveTime(t);
            }
            else if (type is EntityMsgType.PlayNextSkill)
            {
                int skillId = (int)msg;
                roleData.roleControl.TryPlaySkill(skillId);
            }
            else if (type is EntityMsgType.AddTag)
            {
                BaseMsg baseMsg = (BaseMsg)msg;
                if (baseMsg.state == 0)
                {
                    AddTag((int)baseMsg.numMsg);
                }
                else
                {
                    RemoveTag((int)baseMsg.numMsg);
                }
            }
        }

        public virtual void AIMoveTo(Vector3 pos, float speedFactor = 1, bool isLookForward = false)
        {

        }

        public virtual void AIMsg(ActMsgType actType, string actMsg)
        {
            if (actType == ActMsgType.Skill)
            {

            }
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

    public class RoleMgr : Singleton<RoleMgr>, IClearCache
    {
        public Dictionary<int, Role> roleDic = new Dictionary<int, Role>();


        //首先获取所有范围内敌人
        //获取最高分数
        //视觉范围为angle
        //超过视觉范围 做插值剔除  maxDis = Mathf.Lerp(hearR, seeR, angleP);
        //距离越小分数越高 ds = 1/d  (d >0.1)
        //夹角越小分数越高 as = cos(x)
        //旧目标加分计算 暂无
        public Role SearchEnemyRole(Transform self, float seeR, float seeAngle, out float maxS, int team = TeamTag.Enemy)
        {
            float hearR = seeR * 0.4f;
            float angleP = 1;
            Role role = null;
            maxS = 0;
            foreach (var item in roleDic.Values)
            {
                if (item.team != team && !item.IsDie)
                {
                    GetAngleAndDistance(self, item.transform, out float curAngle, out float dis);
                    if (curAngle > seeAngle)
                    {
                        MathTool.ValueMapping(curAngle, seeAngle, 180, 1, 0);
                    }
                    float maxDis = Mathf.Lerp(hearR, seeR, angleP);
                    if (dis < maxDis)
                    {
                        float _ds = 1 / dis;
                        float _as = Mathf.Cos(curAngle / 2f * Mathf.Deg2Rad);
                        float end = _ds * _as;

                        //查找分数最高
                        if (end > maxS)
                        {
                            maxS = end;
                            role = item;
                        }
                    }
                }
            }
            return role;
        }

        //计算两个物体正前方夹角 和距离
        private void GetAngleAndDistance(Transform self, Transform target, out float curAngle, out float dis)
        {
            Vector3 dir = target.position - self.position;

            curAngle = Vector3.Angle(dir, target.forward);

            dis = Mathf.Max(0.1f, dir.magnitude);
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

    public interface IRoleControl
    {
        public void TryPlaySkill(int skillId);

        public bool IsBusy(int level = 0);
        public void SetNoBusy();
    }

    //技能通用体
    public abstract class RoleControl<T> : RoleComponent<T>, IRoleControl where T : Role
    {
        public RoleControl(T _owner) : base(_owner) { }

        public List<XCTaskRunner> curTaskData = new List<XCTaskRunner>();
        public CharacterController cc => owner.idRole.cc;

        public bool IsBusy(int level = 0)
        {
            foreach (var item in curTaskData)
            {
                if (item.IsBusy)
                {
                    return true;
                }
            }
            return false;
        }
        private bool IsHighLevelSkill(int skillId)
        {
            //读表 读配置
            return false;
        }

        //TaskEnd与角色恢复自主移动不同, 如飞行剑气释放完后, 角色恢复控制, 但剑气可以一直运动
        public void OnTaskEnd(XCTaskRunner runner)
        {
            //curTaskData.Remove(runner);
        }
        public void OnBreak()
        {
            foreach (var task in curTaskData)
            {
                task.SetBreak();
            }
        }

        public void SetNoBusy()
        {
            OnBreak();
        }
        public void OnTaskUpdate()
        {
            bool hasStop = false;
            int firstLen = curTaskData.Count;
            for (int i = 0; i < firstLen; i++)
            {
                if (!curTaskData[i].IsAllStop)
                {
                    curTaskData[i].OnUpdate();
                }
                else
                {
                    hasStop = true;
                }
            }


            //移除结束任务
            if (hasStop)
            {
                int len = curTaskData.Count;
                for (int i = len - 1; i > 0; i--)
                {
                    if (curTaskData[i].IsAllStop)
                    {
                        curTaskData.RemoveAt(i);
                    }
                }
            }

        }
        public void OnMainTaskEnd(XCTaskRunner runner)
        {
            if (!runner.IsBreak)
            {
                Data_R.skillState = ESkillState.SkillEnd;
                Debug.Log($"---  OnSkillFinish ");
            }
            else
            {
                Data_R.skillState = ESkillState.Idle;
            }

        }

        public virtual void TryPlaySkill(int skillId)
        {
            //条件判断, 耗蓝等等
            if (!Data_R.IsFree)
                return;
            //排除高优先级技能, 高优先级技能可以在别的技能使用过程中使用
            if (IsBusy() && !IsHighLevelSkill(skillId))
                return;

            RcpPlaySkill(skillId);
        }

        public virtual void RcpPlaySkill(int skillId)
        {
            Data_R.curSkillId = skillId;
            Transform selfTf = owner.transform;
            TaskInfo taskInfo = new TaskInfo()
            {
                role = owner,
                skillId = skillId,
                entityId = owner.id,
                curGO = owner.gameObject,
                curTF = selfTf,
                playerTF = selfTf,
                castEuler = selfTf.eulerAngles,
                castPos = selfTf.position,
                playerAnimator = owner.Anim,
            };
            var task = XCTaskRunner.CreatNew(skillId, owner.RoleType, taskInfo);
            if (task == null)
            {
                return;
            }
            curTaskData.Add(task);
            task.onMainEndEvent.AddListener(OnMainTaskEnd);
            task.onAllTaskEndEvent.AddListener(OnTaskEnd);
            Data_R.skillState = ESkillState.Skill;

        }


        #region FullSkillId
        public int GetSkillIdFull(int index)
        {
            return RaceIdSetting.GetSkillIdFull(owner.raceId, index);
        }
        public int GetNorAckIdFull(int index)
        {
            return RaceIdSetting.GetNorAckIdFull(owner.raceId, index);
        }

        public int GetRollSkillId()
        {
            return RaceIdSetting.GetRollSkillId(owner.raceId);
        }

        #endregion
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

        public ESkillState skillState;

        public BreakState breakState = new BreakState();

        public PlayerAttr playerAttr = new PlayerAttr();

        public RoleState roleState = new RoleState();

        public IRoleControl roleControl;

        //特殊,需要手动赋值
        public MoveSetting moveSetting;

        public RoleMovement movement;
        public bool IsFree => bodyState is not EBodyState.Break or EBodyState.Dead;

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

        public Vector3 inputDir = Vector3.zero;
        public float MoveMultFinal => moveSpeedMult * moveAnimMult;
        public bool IsMoveLock => moveLockFlag || moveLockTime > 0;

        public void Used()
        {
            inputDir = Vector3.zero;
        }

    }

    public class BreakState
    {
        public float armor;  //虽说有小数, 实际用整数
        public float maxArmor;
        public float recoverWait_t;  //进入破防后,多久启动恢复
        public float recoverFinish_t = 0.5f; //恢复满需要时间
        public float recoverSpeedInner = 0; //被动恢复,没陷入破防时的恢复速度,boss用
        public float maxBreakTime = 0;  //最大连续受击时间,默认0为无
        public float recoverSpeed => maxArmor / recoverFinish_t; //每秒回复多少

        public BreakSubState state { get; set; }
        public bool isHover { get; set; }//是否滞空
        public bool isBreak => state != BreakSubState.None;  //是否破防

        public float enterBreakTime;
        public float breakTimer;

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
        }

        public void OnUpdate(float deltaTime)
        {
            DebugGUI.Log("BreakState", state);
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