using cfg;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

public class ItemIdComponent : IdComponent
{
    public override EEntityType EntityType => EEntityType.Item;

    public UnityEvent deadEvent;

    public AudioClip deadClip;

    public virtual void OnDamage(AtkInfo atkInfo)
    {
        HitHelper.ShowDamageText(transform, atkInfo.atk, atkInfo);
    }

}
