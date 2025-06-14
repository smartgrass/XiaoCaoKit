using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

/// <summary>
/// 用于标记id, 用于给其他组件用
/// </summary>
public class IdComponent : MonoBehaviour
{
    [NaughtyAttributes.ReadOnly]
    public int id;

    public virtual EEntityType EntityType { get; }

    protected BehaviorEntity _entity;

    public BehaviorEntity GetEntity()
    {
        if (_entity == null)
        {
            EntityMgr.Inst.FindEntity<BehaviorEntity>(id, out BehaviorEntity entity);
            this._entity = entity;
        }
        return _entity;
    }

    //[Button(enabledMode: EButtonEnableMode.Playmode)]
    //public void AddEntityViewEditor()
    //{
    //    var role = gameObject.GetOrAddComponent<RoleDataViewer>();
    //    role.entity = GetEntity() as Role;
    //}
}


public enum EEntityType
{
    Role,
    Item
}
