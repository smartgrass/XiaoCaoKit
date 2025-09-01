using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UIElements;
using XiaoCao;
using EGameEvent = XiaoCao.EGameEvent;
using System;
using GG.Extensions;


#if UNITY_EDITOR
using UnityEditor;

#endif

public class EnemyCreator : GameStartMono, IExecute
{
    public EnemyCreateMode createMode;

    public bool IsPlayerTeam;

    public GameObject showEffectPrefab;

    public string enemyIdList = "E_0";

    public string skinNameSet;

    [Dropdown(nameof(GetDirAllFileName))] [Label("")] [MiniBtn(nameof(SetEnemyValue), "选中")]
    public string enmeyBrowse;

    [Label("lvConfigIndex")] public int baseLv = 0;

    public int genCount = 1;

    public float delayGen = 0.5f;

    public float circleSize = 1;

    [Multiline] public string taskLines;

    private int curGenCount;
    private int deadCount;
    private List<int> _enemyList = new List<int>();
    public Action<EnemyCreator> enemyAllDead;


    public override void OnGameStart()
    {
        if (createMode is EnemyCreateMode.LoadAI or EnemyCreateMode.LoadNoAI)
        {
            XCTime.DelayRun(delayGen, Gen).ToObservable();
        }
    }

    private string[] GetEnemyNameList()
    {
        List<string> list = new List<string>();
        string[] ids = enemyIdList.Split('&');
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
    [Button("生成/激活已生成AI", enabledMode: EButtonEnableMode.Playmode)]
    public void Gen()
    {
        if (createMode == EnemyCreateMode.LoadNoAI && curGenCount > 0)
        {
            ActiveEnemyAI();
        }
        else
        {
            GenEnemy();
        }
    }


    private void GenEnemy()
    {
        string[] enemyNameList = GetEnemyNameList();

        int posIndex = 0;
        int posCount = genCount * enemyNameList.Length;
        for (int i = 0; i < genCount; i++)
        {
            foreach (string id in enemyNameList)
            {
                Enemy0 enemy = EnemyMaker.Inst.CreatEnemy(id, LevelSettingHelper.GetEnemyLevel(baseLv), skinNameSet);

                var genPos = GetGenPosition(posIndex, posCount);

                ShowEffect(genPos);

                enemy.enemyData.movement.MoveToImmediate(genPos);

                enemy.enemyData.movement.LookToDir(transform.forward);

                enemy.IsAiOn = createMode is EnemyCreateMode.AI or EnemyCreateMode.LoadAI;
                if (createMode == EnemyCreateMode.LoadNoAI)
                {
                    enemy.AddTag(RoleTagCommon.EnableAiIfHurt);
                }

                if (IsPlayerTeam)
                {
                    enemy.SetTeam(XCSetting.PlayerTeam);
                }

                enemy.DeadAct += OnEnemyDead;

                curGenCount++;

                _enemyList.Add(enemy.id);

                posIndex++;

                if (!string.IsNullOrEmpty(taskLines))
                {
                    var action = enemy.idRole.AddComponent<EnemyShowAction>();
                    action.taskLines = taskLines;
                    action.OnEnemyCreat(this, enemy);
                }
            }
        }
    }

    void ActiveEnemyAI()
    {
        foreach (var enemyId in _enemyList)
        {
            if (EntityMgr.Inst.FindEntity<Enemy0>(enemyId, out Enemy0 enemy))
            {
                enemy.IsAiOn = true;
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

        ///<see cref="LevelControl.OnEnemyDeadEvent"/> 并行触发
        Debug.Log($"--- 无奖励");
        //GetItemEffectHelper.PlayRewardEffect(role.transform.position, null);
    }


    private Vector3 GetGenPosition(int index, int count)
    {
        if (count > 0)
        {
            float radius = ((count - 2) * 0.5f + 2) * circleSize;
            Mathf.Clamp(radius, 2, 5);
            var addVec = MathTool.AngleToVector(index * 360 / count + transform.localEulerAngles.y).To3D();
            ;
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
        var obj = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Res/Role/IdRole/{enmeyBrowse}.prefab");
        EditorGUIUtility.PingObject(obj);
#endif
    }

    private List<string> GetDirAllFileName()
    {
        return PathTool.GetDirAllFileName("Assets/_Res/Role/IdRole");
    }
}


public enum EnemyCreateMode
{
    AI,
    LoadNoAI,
    LoadAI,
    NoAI,
}