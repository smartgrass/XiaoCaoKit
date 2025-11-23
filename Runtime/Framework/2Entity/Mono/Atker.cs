using cfg;
using DG.Tweening;
using NaughtyAttributes;
using System;
using System.Diagnostics;
using UnityEngine;
using XiaoCao;
using Debug = UnityEngine.Debug;

///<see cref="RayCasterTrigger"/>
///<see cref="ColliderTrigger"/>
public class Atker : BaseAtker
{
    public override void ReceiveTriggerEnter(Collider other)
    {
        //?
        var rb = other.attachedRigidbody;
        if (rb == null)
        {
            return;
        }

        //只对Trigger造成伤害
        if (!other.isTrigger)
        {
            Debug.LogWarning($"--- other !isTrigger {other.gameObject.name}");
            return;
        }

        if (TryGetTargetRole(rb, out Role role))
        {
            InitHitInfo(other);
            OnHitRole(role);
        }
        else if (rb.CompareTag(Tags.ITEM) && GetEntity().HasTag(RoleTagCommon.MainPlayer) &&
                 rb.TryGetComponent<ItemIdComponent>(out ItemIdComponent item))
        {
            InitHitInfo(other);
            item.OnDamage(ackInfo);
        }
    }

    public void OnHitRole(Role role)
    {
        if (BattleData.IsTimeStop && role.IsPlayer)
        {
            //时停时玩家不受伤害
            return;
        }

        AtkInfoHelper.ProcessAtkInfo(ackInfo);
        role.OnDamage(ackInfo);

        curTriggerTime++;
        if (maxTriggerTime != 0 && curTriggerTime >= maxTriggerTime)
        {
            OnTriggerTimeOut();
        }
    }


    internal void AddTriggerByCollider()
    {
        ColliderTrigger colliderTrigger = gameObject.GetOrAddComponent<ColliderTrigger>();
        colliderTrigger.InitListener(ReceiveTriggerEnter);
    }
}


public abstract class BaseAtker : IdComponent
{
    public int maxTriggerTime;

    [ReadOnly] public int curTriggerTime;

    public AtkInfo ackInfo { get; set; }

    public virtual void ReceiveTriggerEnter(Collider collider)
    {
    }

    //碰撞次数耗光
    public virtual void OnTriggerTimeOut()
    {
        ackInfo.maxTriggerAct?.Invoke();
    }

    public bool TryGetTargetRole(Component component, out Role role)
    {
        if (component.TryGetComponent<IdRole>(out IdRole IdComponent))
        {
            if (IdComponent.GetEntity() is Role entity)
            {
                if (ackInfo == null)
                {
                    Debug.LogError("---  ackInfo null");
                }

                if (entity.team != ackInfo.team && !entity.IsDie)
                {
                    role = entity;
                    return true;
                }
            }
        }

        role = null;
        return false;
    }


    public virtual void InitAtkInfo(AtkInfo info)
    {
        id = info.atker;
        ackInfo = info;
        gameObject.layer = XCSetting.GetTeamAtkLayer(info.team);
    }

    public void InitHitInfo(Collider other)
    {
        ackInfo.ackObjectPos = transform.position;
        ackInfo.hitPos = other.ClosestPointOnBounds(transform.position);
        //hitDir
        //ackInfo.hitDir = transform.right;
        ackInfo.hitDir = transform.parent.forward;

        // Deubg Dir
        //var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //cube.transform.position = other.transform.position;
        //cube.transform.DOMove(cube.transform.position + ackInfo.hitDir * 5, 3f);
        ackInfo.hitDir.y = 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (transform.parent)
        {
            var oldColor = Gizmos.color;
            Gizmos.color = Color.red;
            var dir = transform.parent.forward;
            Gizmos.DrawLine(transform.position, transform.position + dir * 5);
            Gizmos.color = oldColor;
        }
    }
}

///<see cref="TaskInfo"/>
[Serializable]
public class AtkInfo
{
    public int team;
    public int atker; //攻击者
    public int beAtker; //被攻击者
    public int atk = 1; //倍率计算后
    public int baseAtk = 1; //角色基础攻击力.
    public bool isCrit; //暴击

    public string skillId;
    public int subSkillId = 0;

    internal Vector3 hitDir;
    internal Vector3 hitPos;
    internal Vector3 ackObjectPos;
    public Action maxTriggerAct;

    private SkillSetting setting;

    public bool IsLocalPlayer
    {
        get { return atker.IsLocalPlayerId(); }
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

    public void AutoSetAtkValue()
    {
        atk = (int)(baseAtk * GetSkillSetting.AckRate);
    }
}

public class AtkInfoHelper
{
    /// <summary>
    /// <see cref="Atker.ProcessAtkInfo"/>
    /// </summary>
    public static AtkInfo CreatInfo(Role player, string skillId)
    {
        PlayerAttr attr = player.PlayerAttr;
        bool isCrit = MathTool.IsInRandom(attr.Crit / 100f);
        int baseAtk = attr.Atk;
        var info = new AtkInfo()
        {
            team = player.team,
            skillId = skillId,
            baseAtk = baseAtk,
            isCrit = isCrit,
            atker = player.id,
        };
        info.AutoSetAtkValue();
        return info;
    }

    public static void ProcessAtkInfo(AtkInfo ackInfo)
    {
        float GetDamageFactor = BattleData.Current.GetDamageFactor(ackInfo.team);
        ackInfo.atk = (int)(ackInfo.atk * GetDamageFactor);
    }
}