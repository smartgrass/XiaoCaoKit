using cfg;
using System;
using UnityEditor;
using UnityEngine;
using XiaoCao;
/*
 攻击层级, PLAYER_ATK & Layers.ENEMY_ATK
 受击中层级:  PLAYER & ENEMY

 */

public class AtkTrigger : IdComponent
{
    public AtkInfo info;

    public int maxTriggerTime = 0;

    public int curTriggerTime { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null)
        {
            return;
        }



        if (other.attachedRigidbody.TryGetComponent<IdRole>(out IdRole IdRole))
        {
            if (EntityMgr.Inst.FindEntity<Role>(IdRole.id, out Role entity))
            {
                if (entity.team != info.team)
                {
                    if (BattleData.IsTimeStop && entity.IsPlayer)
                    {
                        //时停时玩家不受伤害
                        return;
                    }
                    //击中信息
                    InitHitInfo(other);
                    entity.OnDamage(id, info);

                    curTriggerTime++;
                    if (maxTriggerTime != 0 && curTriggerTime >= maxTriggerTime)
                    {
                        Debug.Log($"--- {transform.parent.name} OnEnd");
                        info.objectData.OnEnd();
                    }
                }
            }
        }

        //EntityType

        if (other.attachedRigidbody.TryGetComponent<ItemIdComponent>(out ItemIdComponent item))
        {
            if (GetEntity().HasTag(RoleTagCommon.MainPlayer))
            {
                InitHitInfo(other);
                item.OnDamage(id, info);
            }
        }
    }

    private void InitHitInfo(Collider other)
    {
        info.ackObjectPos = transform.position;
        info.hitPos = other.ClosestPointOnBounds(transform.position);
        info.hitDir = (info.hitPos - transform.position);
        info.hitDir.y = 0;
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
    public ObjectData objectData;

    public SkillSetting GetSkillSetting
    {
        get
        {
            return LubanTables.GetSkillSetting(skillId, subSkillId);
        }
    }
}

