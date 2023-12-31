﻿using System;
using UnityEngine;
using XiaoCao;

public class AtkTrigger : IdComponent
{
    public AtkInfo info;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IdRole>(out IdRole role))
        {
            if (EntityMgr.Instance.FindEntity<Role>(role.id, out Role entity))
            {
                entity.OnDamage(id,info);
            }
        }
    }
}

[Serializable]
public class AtkInfo
{
    public int atk;
    public bool isCrit; //暴击

}