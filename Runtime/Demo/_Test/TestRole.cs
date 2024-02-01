using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using XiaoCao;
using EventType = XiaoCao.EventType;

public class TestRole : GameStartMono
{
    public RoleType roleType = RoleType.Enemy;

    public int genId = -1;

    public override void OnGameStart()
    {
        if (genId >= 0)
        {
            Gen();
        }
    }

    //��Ҫһ�����ɵ���Ԥ����
    [Button("����")]
    void Gen()
    {
        if (roleType == RoleType.Enemy)
        {
            Enemy0 enemy = EntityMgr.Inst.CreatEntity<Enemy0>();
            enemy.Init();
            enemy.gameObject.transform.position = transform.position;
        }

    }
}
