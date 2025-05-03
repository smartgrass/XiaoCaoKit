using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UIElements;
using XiaoCao;
using EGameEvent = XiaoCao.EGameEvent;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyCreator : GameStartMono, IExecute
{
    public bool autoStart;

    public RoleType roleType = RoleType.Enemy;

    public GameObject showEffectPrefab;

    public string enemyIdList = "1,1";

    [Dropdown(nameof(GetDirAllFileName))]
    [Label("")]
    //[Header("Show all Enmey")]
    [MiniBtn(nameof(SetEnemyValue), "选中")]
    public string enmeyBrowse;

    public int level = 1;

    public int genCount = 1;

    public float delayGen = 0.5f;

    public bool enableAI = true;

    public float circleSize = 1;


    private int curGenCount;
    private int deadCount;
    private List<int> _enemyList = new List<int>();
    public Action<EnemyCreator> enemyAllDead;


    public override void OnGameStart()
    {
        if (autoStart)
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

    public void Execute()
    {
        Gen();
    }

    //需要一个生成敌人预制体
    [Button("生成", enabledMode: EButtonEnableMode.Playmode)]
    public void Gen()
    {
        if (roleType == RoleType.Enemy)
        {
            string[] enemyList = GetEnemyList();

            int posIndex = 0;
            int posCount = genCount * enemyList.Length;
            for (int i = 0; i < genCount; i++)
            {
                foreach (string id in enemyList)
                {

                    Enemy0 enemy = EnemyMaker.Inst.CreatEnemy(id, level);

                    var genPos = GetGenPosition(posIndex, posCount);

                    ShowEffect(genPos);

                    enemy.enemyData.movement.MoveToImmediate(genPos);

                    enemy.enemyData.movement.LookToDir(transform.forward);

                    enemy.IsAiOn = enableAI;

                    enemy.DeadAct += OnEnemyDead;

                    curGenCount++;

                    _enemyList.Add(enemy.id);

                    posIndex++;
                }
            }
        }

    }

    void ShowEffect(Vector3 pos)
    {
        if (showEffectPrefab)
        {
            var pool = PoolMgr.Inst.GetOrCreatPrefabPool(showEffectPrefab);
            pool.Get().transform.position = pos;
        }
    }

    private void OnEnemyDead(Role role)
    {
        //TODO临时写法 有需求再优化
        deadCount++;
        _enemyList.Remove(role.id);
        if (deadCount == curGenCount)
        {
            enemyAllDead?.Invoke(this);
        }
        GetItemEffectHelper.GetItem(role.transform.position);
    }


    private Vector3 GetGenPosition(int index, int count)
    {
        if (count > 0)
        {
            float radius = ((count - 2) * 0.5f + 2) * circleSize;
            Mathf.Clamp(radius, 2, 5);
            var addVec = MathTool.AngleToVector(index * 360 / count).To3D(); ;
            return transform.position + addVec * radius;
        }
        else
        {
            return transform.position;
        }
    }


    void SetEnemyValue()
    {
        Debug.Log($"--- {enmeyBrowse}");
        enemyIdList = enmeyBrowse;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        var obj = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Res/Role/Enemy/{enmeyBrowse}.prefab");
        EditorGUIUtility.PingObject(obj);
#endif
    }

    private List<string> GetDirAllFileName()
    {
        return PathTool.GetDirAllFileName("Assets/_Res/Role/Enemy");
    }


}
