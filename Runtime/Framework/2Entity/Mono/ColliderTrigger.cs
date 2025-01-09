using System;
using System.Drawing;
using UnityEngine;
/*
 攻击层级, PLAYER_ATK & Layers.ENEMY_ATK
 受击中层级:  PLAYER & ENEMY

通过Trigger接受信息
 */

///<see cref="RayCasterTrigger"/>
public class ColliderTrigger : MonoBehaviour, ITrigger
{
    public Action<Collider> TriggerAct { get; set; }

    void OnTriggerEnter(Collider other)
    {
        TriggerAct?.Invoke(other);
    }

    public void InitListener(Action<Collider> action)
    {
        TriggerAct = null;
        TriggerAct += action; 
    }

    public void Switch(bool isOn)
    {
        enabled = isOn;
    }
}

public interface ITrigger
{
    public Action<Collider> TriggerAct { get; set;}

    void InitListener(Action<Collider> action);

    void Switch(bool v);
}
