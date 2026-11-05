using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

/// <summary>
/// 触发结束回调: 子弹/投掷物命中或飞行结束时在当前位置生成爆炸特效, 并按 lifeTime 回收到对象池
/// </summary>
public class TriggerEndExplode : MonoBehaviour, ITriggerEnd
{
    public GameObject prefab;

    public AudioClip audioClip;

    /// <summary>特效存活时间, 到点回收到对象池; 0 表示不自动回收</summary>
    [Tooltip("特效存活时间, 到点回收到对象池(0 = 不回收)")]
    public float lifeTime = 2f;

    [Header("爆炸伤害")]
    [Tooltip("勾选后爆炸会对半径内的敌人造成伤害\n(爆炸特效自身挂了 Atker 时以 Atker 的碰撞触发为准, 不会重复结算)")]
    public bool dealDamage;

    [Tooltip("爆炸伤害半径(米)")]
    [MinValue(0.1f)]
    public float damageRadius = 2f;

    [Tooltip("固定伤害, 大于 0 时优先使用")]
    public int damage;

    [Tooltip("伤害倍率, damage 为 0 时按 本次命中伤害 * 倍率 计算")]
    [MinValue(0f)]
    public float damageRate = 1f;

    [Tooltip("伤害生效延迟(秒), 让特效先炸开; 0 = 立即生效")]
    [MinValue(0f)]
    public float damageDelay = 0.05f;

    [Tooltip("勾选后本次命中的目标不再吃爆炸伤害, 避免「子弹 + 爆炸」双倍结算")]
    public bool excludeHitTarget = true;

    public void OnTriggerEnd(BaseAtker atker)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"--- TriggerEndExplose prefab null {name}");
            return;
        }

        AssetPool pool = PoolMgr.Inst.GetOrCreatPrefabPool(prefab);
        GameObject instance = pool.Get();
        SoundMgr.Inst.PlayClip(audioClip);
        Transform instanceTran = instance.transform;
        instanceTran.SetPositionAndRotation(transform.position, transform.rotation);

        //对象池不会自动回收, 按生命期还给池子, 否则每次命中都会留下一个常驻特效
        if (lifeTime > 0)
        {
            XCTime.DelayTimer(lifeTime, () => pool.Release(instance));
        }

        //特效自身带 Atker 时, 伤害交给它的碰撞触发结算
        BaseAtker triggerAtker = instance.GetComponentInChildren<BaseAtker>();
        if (triggerAtker != null)
        {
            AtkInfo triggerInfo = GetAtkInfo(atker);
            if (triggerInfo == null)
            {
                return;
            }

            triggerAtker.InitAtkInfo(triggerInfo);
            if (triggerAtker is Atker colliderAtker)
            {
                colliderAtker.AddTriggerByCollider();
            }

            return;
        }

        if (!dealDamage)
        {
            return;
        }

        AtkInfo damageInfo = GetAtkInfo(atker);
        if (damageInfo == null)
        {
            return;
        }

        SetRangeDamage(damageInfo);

        //伤害数据先复制好再延迟, 此时子弹可能已被回收
        Vector3 explodePos = instanceTran.position;
        if (damageDelay > 0)
        {
            XCTime.DelayTimer(damageDelay, () => ApplyRangeDamage(damageInfo, explodePos));
        }
        else
        {
            ApplyRangeDamage(damageInfo, explodePos);
        }
    }

    /// <summary>
    /// 按固定伤害/倍率换算本次爆炸伤害; 源数据还没结算过伤害系数时先补一次, 避免重复乘算
    /// </summary>
    private void SetRangeDamage(AtkInfo info)
    {
        AtkInfoHelper.ProcessAtkInfo(info);
        info.atk = damage > 0 ? damage : Mathf.RoundToInt(info.atk * damageRate);
    }

    /// <summary>
    /// 对爆炸半径内的敌人逐一生效 (以爆炸中心计算受击方向与推力)
    /// </summary>
    private void ApplyRangeDamage(AtkInfo info, Vector3 pos)
    {
        if (info == null)
        {
            return;
        }

        List<Role> targets = RoleMgr.Inst.SearchEnemyInRadius(pos, damageRadius, info.team);
        if (targets.Count == 0)
        {
            return;
        }

        info.ackObjectPos = pos;
        //命中目标已经吃过子弹伤害, 默认不再参与爆炸结算, 保证每个敌人只掉一次血
        int hitTargetId = excludeHitTarget ? info.beAtker : 0;
        for (int i = 0; i < targets.Count; i++)
        {
            Role role = targets[i];
            if (hitTargetId != 0 && role.id == hitTargetId)
            {
                continue;
            }

            if (BattleData.IsTimeStop && role.IsPlayer)
            {
                //时停时玩家不受伤害, 与 Atker.OnHitRole 保持一致
                continue;
            }

            info.beAtker = role.id;
            info.hitPos = role.transform.position;
            Vector3 dir = role.transform.position - pos;
            dir.y = 0;
            info.hitDir = dir;
            role.OnDamage(info);
        }
    }

    private static AtkInfo GetAtkInfo(BaseAtker atker)
    {
        if (atker?.ackInfo == null)
        {
            Debug.LogWarning("--- TriggerEndExplose atker ackInfo null");
            return null;
        }

        AtkInfo source = atker.ackInfo;
        return new AtkInfo()
        {
            team = source.team,
            atker = source.atker,
            beAtker = source.beAtker,
            atk = source.atk,
            baseAtk = source.baseAtk,
            isCrit = source.isCrit,
            critDamage = source.critDamage,
            isDamageProcessed = source.isDamageProcessed,
            skillId = source.skillId,
            subSkillId = source.subSkillId,
        };
    }

    private void OnDrawGizmosSelected()
    {
        if (!dealDamage)
        {
            return;
        }

        Color oldColor = Gizmos.color;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, damageRadius);
        Gizmos.color = oldColor;
    }
}
