using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

/// <summary>
/// 破敌范围执行脚本: 触发时在当前位置生成特效, 并对半径内的敌人造成伤害
/// (配合 PlayerEnterTriggerChecker / ItemCrystal 等触发调用 Execute)
/// </summary>
public class BreakRangeEnemyExce : MonoExecute
{
    [Tooltip("特效预制体")]
    public GameObject effectPrefab;

    [Tooltip("特效存活时间(秒), 0 表示不自动回收")]
    [MinValue(0)]
    public float effectLife = 1.5f;
    
    [Tooltip("伤害配置")]
    public string skillId = "Break";

    [Tooltip("伤害半径")]
    [MinValue(0.1f)]
    public float radius = 3f;

    [Tooltip("固定伤害, 大于 0 时优先使用")]
    public int damage;

    [Tooltip("伤害倍率, damage 为 0 时按 玩家攻击力*倍率 计算")]
    public float damageRate = 1f;

    public override void Execute()
    {
        Vector3 pos = transform.position;
        PlayEffect(pos);

        var player = GameDataCommon.GetPlayer();
        List<Role> enemies = RoleMgr.Inst.SearchEnemyInRadius(pos, radius, player.team);
        if (enemies.Count == 0)
        {
            return;
        }

        AtkInfo atkInfo = CreateAtkInfo(player);
        //爆炸中心, 用于计算受击方向与推力的衰减
        atkInfo.ackObjectPos = pos;
        atkInfo.skillId = skillId;

        foreach (Role enemy in enemies)
        {
            atkInfo.beAtker = enemy.id;
            atkInfo.hitPos = enemy.transform.position;

            Vector3 dir = enemy.transform.position - pos;
            dir.y = 0;
            atkInfo.hitDir = dir;

            enemy.OnDamage(atkInfo);
        }
    }

    /// <summary>
    /// 在指定位置播放特效, 按 effectLife 自动回收到对象池
    /// </summary>
    private void PlayEffect(Vector3 pos)
    {
        AssetPool pool = PoolMgr.Inst.GetOrCreatPrefabPool(effectPrefab);
        GameObject effect = pool.Get();
        effect.transform.position = pos;
        effect.transform.rotation = transform.rotation;
        effect.SetActive(true);

        if (effectLife > 0)
        {
            XCTime.DelayTimer(effectLife, () => pool.Release(effect));
        }
    }

    /// <summary>
    /// 构造本次范围伤害的攻击数据, 未配置固定伤害时按玩家攻击力换算
    /// </summary>
    private AtkInfo CreateAtkInfo(Role player)
    {
        AtkInfo atkInfo = AtkInfoHelper.CreatInfo(player, string.Empty);
        atkInfo.atk = damage > 0 ? damage : Mathf.RoundToInt(player.RoleAttr.Atk * damageRate);
        AtkInfoHelper.ProcessAtkInfo(atkInfo);
        return atkInfo;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
