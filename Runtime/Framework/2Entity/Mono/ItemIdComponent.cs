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

    public AudioClip deadClip;

    [ReadOnly]
    public DeadInfo deadInfo;

    public virtual void OnDamage(AtkInfo atkInfo)
    {
        HitHelper.ShowDamageText(transform, atkInfo.atk, atkInfo);
    }

}
