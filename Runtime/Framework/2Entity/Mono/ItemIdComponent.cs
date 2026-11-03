using cfg;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

public class ItemIdComponent : IdComponent
{
    public override EEntityType EntityType => EEntityType.Item;

    public UnityEvent deadEvent;

    public string deadMapMsg;

    public AudioClip deadClip;

    [ReadOnly]
    public DeadInfo deadInfo;
    
    public bool noHurt;

    [Tooltip("允许敌人(非玩家阵营)的攻击打到自己; 不勾时只有玩家能打")]
    public bool allowEnemyAttack;

    /// <summary>
    /// 该阵营的攻击能不能伤到自己: 默认只有玩家阵营,
    /// 场景家具之类需要被敌人顺手打坏的, 在 Inspector 勾 allowEnemyAttack
    /// </summary>
    public virtual bool CanBeAttackedBy(int team)
    {
        return team == XCSetting.PlayerTeam || allowEnemyAttack;
    }

    public virtual void OnDamage(AtkInfo atkInfo)
    {
        HitHelper.ShowDamageText(transform, atkInfo.atk, atkInfo);
    }

    public virtual void ToDead()
    {
        Debug.Log($"-- dead {this.gameObject.name} but nothing");
    }

}
