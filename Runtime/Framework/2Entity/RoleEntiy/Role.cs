
using UnityEditor;
using UnityEngine;

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

            idRole = go.transform.GetComponent<IdRole>();
            if (idRole == null)
            {
                //如果无IdRole, 则需要加载模板
                string baseRole = XCPathConfig.GetRoleBasePath(RoleType);
                GameObject baseGo = ResMgr.LoadInstan(baseRole);
                idRole = baseGo.transform.GetComponent<IdRole>();
                idRole.id = id;
                go.transform.SetParent(baseGo.transform, false);
                body = go;
                BindGameObject(baseGo);
            }
            else
            {
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

        }
    }
  

    public static class RoleTagCommon
    {
        public const int ShowHp = 0;
        public const int MainPlayer = 1;
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