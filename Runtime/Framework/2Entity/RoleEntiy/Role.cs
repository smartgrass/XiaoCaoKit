
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using TEngine;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace XiaoCao
{
    [TypeLabel(typeof(RoleTagCommon))]
    public abstract class Role : HealthBehavior
    {


        public abstract RoleType RoleType { get; }
        public virtual IData data { get; }
        public virtual IShareData componentData { get; }

        public int raceId = 0; //职业,种族

        public int prefabID = 0;

        public GameObject body;
        internal Animator Anim => idRole.animator;
        public bool isBodyCreated => body != null;

        public IdRole idRole;

        public RoleData roleData = new RoleData();



        public virtual void CreateGameObject(bool isGen = false)
        {
            GenRoleBody(prefabID);
        }
        //皮肤拆分 - body , 不需要动画机
        //玩家需要拆分, 敌人不需要
        protected void GenRoleBody(int prefabId)
        {
            string path = XCPathConfig.GetRolePrefabPath(RoleType, prefabId);

            GameObject go = ResMgr.LoadInstan(path);

            Debuger.Log($"--- path {path}");


            //如果无IdRole, 则需要加载模板
            string baseRole = XCPathConfig.GetRoleBasePath(RoleType);
            GameObject baseGo = ResMgr.LoadInstan(baseRole);
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


        public virtual void OnDamage(int atker, AtkInfo atkInfo)
        {
            //血量计算 toHp
            //死亡处理: 关闭行为,发送消息
            //受击处理: 受击动作, 僵直时间计算
            if (BaseDamageCheck(atkInfo))
            {
                roleData.breakState.OnHit(1);
                if (roleData.breakState.isBreak)
                {
                    
                }
            }
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
            if (!IsAlive || roleData.bodyState == EBodyState.Dead)
            {
                roleData.bodyState = EBodyState.Dead;
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

    }

    public class RoleMgr : Singleton<RoleMgr>, IClearCache
    {
        public Dictionary<int, Role> roleDic = new Dictionary<int, Role>();
    }
    public class RoleData
    {

        public int curSkillId;

        public EBodyState bodyState;

        public BreakState breakState = new BreakState();

        public PlayerAttr playerAttr = new PlayerAttr();

        public RoleState roleState = new RoleState();
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

    public static class RoleTagCommon
    {
        public const int NoHpBar = 0;
        public const int MainPlayer = 1;
        public const int Boss = 2;
    }

    public class PlayerBase : Role
    {
        public override RoleType RoleType => RoleType.Player;

    }


    public class EnemyBase : Role
    {
        public override RoleType RoleType => RoleType.Enemy;
    }


}