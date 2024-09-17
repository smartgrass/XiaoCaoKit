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

    public int enemyId = -1;

    public bool enableAI = true;

    public override void OnGameStart()
    {
        if (enemyId >= 0)
        {
            Gen();
        }
    }

    //需要一个生成敌人预制体
    [Button("生成")]
    void Gen()
    {
        if (roleType == RoleType.Enemy)
        {
            //Enemy0 enemy = EntityMgr.Inst.CreatEntity<Enemy0>();
            //enemy.Init(raceId, enemyId);

            Enemy0 enemy = EnemyMaker.Inst.CreatEnemy(enemyId);

            //enemy.gameObject.transform.position = transform.position;
            enemy.enemyData.movement.MoveToImmediate(transform.position);

            enemy.IsAiOn = enableAI;
            //enemy.enemyData

            //enemy.idRole.rb.MovePosition(transform.position);
            //TODO ai 设置
            //皮肤
        }

    }
}
