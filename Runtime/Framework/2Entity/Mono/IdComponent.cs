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

    public BehaviorEntity GetEntity()
    {
        EntityMgr.Inst.FindEntity<BehaviorEntity>(id, out BehaviorEntity entity);
        return entity;
    }
}


public enum EEntityType
{
    Role,
    Item
}
