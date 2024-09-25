using System;
using UnityEngine;
using XiaoCao;

public class AtkTrigger : IdComponent
{
    public AtkInfo info;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IdRole>(out IdRole IdRole))
        {
            if (EntityMgr.Inst.FindEntity<Role>(IdRole.id, out Role entity))
            {
                if (entity.team != info.team)
                {
                    if (BattleData.Current.IsTimeStop && entity.IsPlayer)
                    {
                        //时停时玩家不受伤害
                        return;
                    }

                    //阵营判断
                    info.ackObjectPos = transform.position;
                    info.hitPos = other.ClosestPointOnBounds(transform.position);
                    info.hitDir = (info.hitPos - transform.position);
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
    public int atk = 1;
    public bool isCrit; //暴击

    public int skillId = 0;
    public int subSkillId = 0;

    internal Vector3 hitDir;
    internal Vector3 hitPos;
    internal Vector3 ackObjectPos;
}

