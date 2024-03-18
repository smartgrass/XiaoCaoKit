using System;
using UnityEngine;
using XiaoCao;

public class AtkTrigger : IdComponent
{
    public AtkInfo info;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IdRole>(out IdRole role))
        {
            if (EntityMgr.Inst.FindEntity<Role>(role.id, out Role entity))
            {
                if (entity.team != info.team)
                {
                    //阵营判断
                    info.ackObjectPos = transform.position;
                    info.hitPos = other.ClosestPointOnBounds(transform.position);
                    info.hitDir = transform.parent.forward;
                    info.hitDir.y = 0;
                    entity.OnDamage(id, info);
                }
            }
        }
    }
}

[Serializable]
public class AtkInfo
{
    public int team;
    public int atk;
    public bool isCrit; //暴击

    public int skillId = 0;
    public int subSkillId = 0;

    internal Vector3 hitDir;
    internal Vector3 hitPos;
    internal Vector3 ackObjectPos;
}

