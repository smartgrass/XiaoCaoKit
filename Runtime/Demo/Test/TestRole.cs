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

    public string enemyIdList = "1,1";

    [Dropdown(nameof(GetDirAllFileName))]
    [Label("")]
    [Header("Show all Enmey")]
    public string enmeyBrowse;

    public int level = 1;

    public int genCount = 1;

    public float delayGen = 0.5f;

    public bool enableAI = true;

    public float circleSize = 1;

    private List<string> GetDirAllFileName()
    {
        return PathTool.GetDirAllFileName("Assets/_Res/Role/Enemy");
    }

    public override void OnGameStart()
    {
        XCTime.DelayRun(delayGen, Gen).ToObservable();
    }

    private string[] GetEnemyList()
    {
        List<string> list = new List<string>();
        string[] ids = enemyIdList.Split(',');
        if (ids.Length == 0)
        {
            list.Add(enemyIdList);
            return list.ToArray();
        }
        return ids;
    }

    //需要一个生成敌人预制体
    [Button("生成", enabledMode: EButtonEnableMode.Playmode)]
    public void Gen()
    {
        if (roleType == RoleType.Enemy)
        {
            string[] enemyList = GetEnemyList();

            int pos = 0;
            int posCount = genCount * enemyList.Length;
            for (int i = 0; i < genCount; i++)
            {
                foreach (string id in enemyList)
                {

                    Enemy0 enemy = EnemyMaker.Inst.CreatEnemy(id,level);

                    enemy.enemyData.movement.MoveToImmediate(GetGenPosition(pos, posCount));

                    enemy.enemyData.movement.LookToDir(transform.forward);

                    enemy.IsAiOn = enableAI;

                    enemy.DeadAct += OnEnemyDead;

                    curGenCount++;
                    
                    _enemyList.Add(enemy.id);
                    
                    pos++;
                }
            }
        }

    }

    private int curGenCount;
    private int deadCount;
    public string groupName;
    private List<int> _enemyList = new List<int>();

    private void OnEnemyDead(Role role){
        //TODO临时写法 有需求再优化
        deadCount++;
        _enemyList.Remove(role.id);
        if (deadCount == curGenCount){
            GameEvent.Send(EventType.EnemyGroupEndEvent.Int(), groupName);
        }
    }


    private Vector3 GetGenPosition(int index,int count)
    {
        if (count > 0)
        {
            float radius = ((count - 2) *0.5f + 2) * circleSize;
            Mathf.Clamp(radius, 2, 5);
            var addVec = MathTool.AngleToVector(index * 360 / count).To3D(); ;
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
