using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TEngine;
using UnityEngine;
using UnityEngine.UIElements;
using XiaoCao;
using EventType = XiaoCao.EventType;

public class TestRole : GameStartMono
{
    public RoleType roleType = RoleType.Enemy;

    public int enemyId = -1;

    public int genCount = 1;

    public float delayGen = 0.5f;

    public bool enableAI = true;

    public override void OnGameStart()
    {
        if (enemyId >= 0)
        {
            XCTime.DelayRun(delayGen, Gen).ToObservable();
        }
    }

    //需要一个生成敌人预制体
    [Button("生成")]
    void Gen()
    {
        if (roleType == RoleType.Enemy)
        {
            for (int i = 0; i < genCount; i++)
            {
                Enemy0 enemy = EnemyMaker.Inst.CreatEnemy(enemyId);

                enemy.enemyData.movement.MoveToImmediate(GetGenPosition(i));

                enemy.IsAiOn = enableAI;
            }
        }

    }

    private Vector3 GetGenPosition(int index)
    {
        if (genCount > 0)
        {
            float radius = (genCount - 2) *0.5f + 2;
            Mathf.Clamp(radius, 2, 5);
            var addVec = MathTool.AngleToVector(index * 360 / genCount).To3D(); ;
            return transform.position + addVec * radius;
        }
        else
        {
            return transform.position;
        }
    }


    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawSphere(transform.position,0.3f);
    //}

}
