
using UnityEditor;
using UnityEngine;

namespace XiaoCao
{

    public abstract class Role : HealthBehavior
    {
        ///<see cref="RoleTypeCode"/>
        public abstract RoleTypeCode RoleType { get; }
        public virtual IData data { get; }
        public virtual IShareData componentData { get; }

        public int prefabID = 0;
        public virtual void CreateGameObject(bool isGen = false)
        {
            if (isGen)
            {
                CreateRoleGameObject(prefabID);
            }
            else
            {
                GenRoloe(prefabID);
            }

        }
        //皮肤拆分 - body , 不需要动画机
        //
        protected void CreateRoleGameObject(int prefabId)
        {
            string path = $"{ResMgr.RESDIR}/Role/{RoleType}/{RoleType}{prefabId}.prefab";
            var task = ResMgr.Loader.LoadAssetSync<GameObject>(path);
            GameObject playeGo = GameObject.Instantiate(task.AssetObject) as GameObject;
            BindGameObject(playeGo);
        }

        protected void GenRoloe(int prefabId)
        {

            string path = $"{ResMgr.RESDIR}/Role/{RoleType}/{RoleType}{prefabId}.prefab";
            var task = ResMgr.Loader.LoadAssetSync<GameObject>(path);
            GameObject playeGo = GameObject.Instantiate(task.AssetObject) as GameObject;

            //RoleUsing
            RoleUsing ru = playeGo.GetComponent<RoleUsing>();


            BindGameObject(playeGo);
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

    public class RoleUsing : MonoBehaviour
    {
        public Animator animator;
    }

    public class Player : Role
    {
        public override RoleTypeCode RoleType => RoleTypeCode.Player;

    }


    public class Enemy : Role
    {
        public override RoleTypeCode RoleType => RoleTypeCode.Enemy;
    }


}