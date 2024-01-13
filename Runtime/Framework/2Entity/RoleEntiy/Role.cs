
using UnityEditor;
using UnityEngine;

namespace XiaoCao
{
    [TypeLabel(typeof(RoleTagCommon))]
    public abstract class Role : HealthBehavior
    {
        public abstract RoleTypeCode RoleType { get; }
        public virtual IData data { get; }
        public virtual IShareData componentData { get; }

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
            string path = $"{ResMgr.RESDIR}/Role/{RoleType}/{RoleType}{prefabId}.prefab";
            GameObject go = ResMgr.LoadInstan(path);

            Debuger.Log($"--- path {path}");

            idRole = go.transform.GetComponent<IdRole>();
            if (idRole == null)
            {
                //如果无, 则需要加载模板
                string baseRole = $"{ResMgr.RESDIR}/Role/{RoleType}/{RoleType}.prefab";
                GameObject baseGo = ResMgr.LoadInstan(baseRole);
                idRole = baseGo.transform.GetComponent<IdRole>();
                idRole.id = id;
                go.transform.SetParent(baseGo.transform,false);
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

    /// <summary>
    /// 比较枚举,使用int的好处,序列化方便
    /// 而使用odin序列化则没啥问题
    /// </summary>
    public enum RoleTypeCode
    {
        Enemy = 0,
        Player = 1,
    }

    public static class RoleTagCommon
    {
        public const int ShowHp = 0;
        public const int MainPlayer = 1;
    }


    public class PlayerBase : Role
    {
        public override RoleTypeCode RoleType => RoleTypeCode.Player;

    }


    public class EnemyBase : Role
    {
        public override RoleTypeCode RoleType => RoleTypeCode.Enemy;
    }


}