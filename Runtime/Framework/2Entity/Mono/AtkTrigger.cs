using cfg;
using System;
using System.Diagnostics;
using UnityEngine;
using XiaoCao;
using Debug = UnityEngine.Debug;
/*
 攻击层级, PLAYER_ATK & Layers.ENEMY_ATK
 受击中层级:  PLAYER & ENEMY

 */

public class AtkTrigger : IdComponent
{
    public AtkInfo ackInfo;

    public int maxTriggerTime = 0;

    public int curTriggerTime { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null)
        {
            return;
        }
        if (!other.isTrigger)
        {
            Debug.LogWarning($"--- other !isTrigger {other.gameObject.name}");
            return;
        }


        if (rb.TryGetComponent<IdRole>(out IdRole IdRole))
        {
            if (EntityMgr.Inst.FindEntity<Role>(IdRole.id, out Role entity))
            {
                if (entity.team != ackInfo.team && !entity.IsDie)
                {
                    if (BattleData.IsTimeStop && entity.IsPlayer)
                    {
                        //时停时玩家不受伤害
                        return;
                    }
                    if (ackInfo.IsLocalPlayer)
                    {
                        var setting = ackInfo.GetSkillSetting;
                        CamEffectMgr.Inst.CamShakeEffect(setting.ShakeLevel);
                    }

                    //击中信息
                    InitHitInfo(other);
                    entity.OnDamage(ackInfo);

                    curTriggerTime++;
                    if (maxTriggerTime != 0 && curTriggerTime >= maxTriggerTime)
                    {
                        Debug.Log($"--- {transform.parent.name} OnEnd");
                        ackInfo.objectData.OnEnd();
                    }
                }
            }
        }

        //EntityType

        if (rb.CompareTag(Tags.ITEM) && GetEntity().HasTag(RoleTagCommon.MainPlayer) &&
            rb.TryGetComponent<ItemIdComponent>(out ItemIdComponent item))
        {
            InitHitInfo(other);
            item.OnDamage(ackInfo);
        }
    }

    private void InitHitInfo(Collider other)
    {
        ackInfo.ackObjectPos = transform.position;
        ackInfo.hitPos = other.ClosestPointOnBounds(transform.position);
        ackInfo.hitDir = (ackInfo.hitPos - transform.position);
        ackInfo.hitDir.y = 0;
    }

}

[Serializable]
public class AtkInfo
{
    public int team;
    public int atker; //攻击者
    public int atk = 1; //倍率计算后
    public int baseAtk = 1;  //角色基础攻击力.
    public bool isCrit; //暴击

    public string skillId;
    public int subSkillId = 0;

    internal Vector3 hitDir;
    internal Vector3 hitPos;
    internal Vector3 ackObjectPos;
    public ObjectData objectData;

    private SkillSetting setting;

    public bool IsLocalPlayer
    {
        get
        {
            return atker.IsLocalPlayerId();
        }
    }


    public SkillSetting GetSkillSetting
    {
        get
        {
            if (setting == null)
            {
                return LubanTables.GetSkillSetting(skillId, subSkillId);
            }
            return setting;
        }
    }
}

