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

                info.hitPos = other.ClosestPointOnBounds(transform.position);
                info.hitDir = transform.forward;
                entity.OnDamage(id,info);
            }
        }
    }
}

[Serializable]
public class AtkInfo
{
    public int atk;
    public bool isCrit; //暴击

    public int skillId = 0;
    public int subSkillId = 0;

    internal Vector3 hitDir;
    internal Vector3 hitPos;
}

