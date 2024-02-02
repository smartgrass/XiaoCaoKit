
using cfg;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using TEngine;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UIElements;

namespace XiaoCao
{
    [TypeLabel(typeof(RoleTagCommon))]
    public abstract class Role : HealthBehavior
    {
        public abstract RoleType RoleType { get; }
        public virtual IData data { get; }
        public virtual IShareData componentData { get; }

        public override bool IsDie => roleData.bodyState == EBodyState.Dead;

        /// <summary>
        ///职业,种族: 每一个角色和敌人都有对应的 raceId
        ///和技能id规划有关  <see cref="PlayerSetting"/>
        /// </summary>
        public int raceId = 0;

        /// <summary>
        /// 绑定的资源
        /// </summary>
        public int prefabID = 0;

        /// <summary>
        /// 势力,相同为友军
        /// </summary>
        public int team = 0;

        public GameObject body;

        public IdRole idRole;

        public RoleData roleData = new RoleData();


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
            idRole = baseGo.transform.GetComponent<IdRole>();
            idRole.id = id;
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
            //血量计算 toHp
            //死亡处理: 关闭行为,发送消息
            //受击处理: 受击动作, 僵直时间计算
            if (BaseDamageCheck(ackInfo))
            {
                var setting = LubanTables.GetSkillSetting(ackInfo.skillId, ackInfo.subSkillId);
                roleData.breakState.OnHit((int)setting.BreakPower);


                if (roleData.breakState.isBreak)
                {
                    Anim.Play(AnimNames.Break);

                    Vector3 horVec = MathTool.Rotate(ackInfo.hitDir, setting.HorForward);
                    idRole.cc.DOHit(setting.AddY, horVec, setting.NoGravityT);
                    transform.RotaToPos(ackInfo.hitPos, 0.5f);


                    // 打断当前技能
                    OnBreak();

                }

                //playerMover.SetNoGravityT(setting.NoGravityT);
            }
        }

        public virtual void OnBreak()
        {

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



        public void RoleIn()
        {
            RoleMgr.Inst.roleDic.Add(id, this);
            GameEvent.Send<int, RoleChangeType>(EventType.RoleChange.Int(), id, RoleChangeType.Add);
        }

        public void RoleOut()
        {
            GameEvent.Send<int, RoleChangeType>(EventType.RoleChange.Int(), id, RoleChangeType.Remove);
            RoleMgr.Inst.roleDic.Remove(id);
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

    }

    public class EnemyBase : Role
    {
        public override RoleType RoleType => RoleType.Enemy;
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
                if (item.team != team && item.IsDie)
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
    //技能通用体
    public abstract class RoleControl<T> : RoleComponent<T> where T : Role
    {
        public RoleControl(T _owner) : base(_owner) { }

        public List<XCTaskRunner> curTaskData = new List<XCTaskRunner>();
        public CharacterController cc => owner.idRole.cc;

        public bool IsBusy(int level = 0)
        {
            foreach (var item in curTaskData)
            {
                //&& item.Task.Info.skillId > 0 TODO
                //break的话, 不算busy
                if (item.Task.IsBusy)
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
        public void OnTaskEnd(XCTaskRunner runner)
        {
            curTaskData.Remove(runner);
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
            curTaskData.Add(task);
            task.onEndEvent.AddListener(OnTaskEnd);
        }


        public void OnBreak()
        {
            foreach (var task in curTaskData)
            {
                task.SetBreak();
            }
        }

        #region FullSkillId
        public int GetFullSkillId(int index)
        {
            return RaceIdSetting.GetFullSkillId(owner.raceId, index);
        }
        public int GetFullNorAckId(int index)
        {
            return RaceIdSetting.GetFullNorAckId(owner.raceId, index);
        }

        public int GetRollId()
        {
            return RaceIdSetting.GetRollId(owner.raceId);
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

        public BreakState breakState = new BreakState();

        public PlayerAttr playerAttr = new PlayerAttr();

        public RoleState roleState = new RoleState();

        //特殊,需要手动赋值
        public MoveSetting moveSetting;
        public RoleMovement movement;
        public bool IsFree => bodyState is not EBodyState.Break or EBodyState.Dead;

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

        public Vector3 moveDir = Vector3.zero;
        public float MoveMultFinal => moveSpeedMult * moveAnimMult;
        public bool IsMoveLock => moveLockFlag || moveLockTime > 0;

        public void Used()
        {
            moveDir = Vector3.zero;
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
                Debug.Log($"--- recover finish");
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
    public enum EAckState
    {
        None,
        NorAck,
        Skill
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