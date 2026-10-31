using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;
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

    public int enemyAddLevel = 0; //见关卡配置,一般+1等于+5; 敌人池表的 addEnemyInfos 会按子节点顺序覆盖这里的值

    public int genCount = 1;

    public float delayGen = 0.5f;

    public float circleSize = 1;

    [Multiline] public string taskLines;

    [Dropdown(nameof(GetShowActKeyList))] [OnValueChanged(nameof(OnTestShowActKeyChange))] [Label("ActKey")]
    public string testShowActKey;

    private int curGenCount;
    private int deadCount;

    //预生成还没轮到就被触发过: 生成出来时直接带AI, 不用等下次激活
    private bool _activateAiOnGen;
    private List<int> _enemyList = new List<int>();
    public Action<EnemyCreator> enemyAllDead;

    public Role TargetRole { get; set; }

    /// <summary>
    /// 进场时就要预生成的模式: 敌人在 OnGameStart 阶段由队列生成好(LoadNoAI / LoadSleepNoAI 生成时关闭AI)
    /// </summary>
    public bool IsPreLoadMode => createMode is EnemyCreateMode.LoadAI or EnemyCreateMode.LoadNoAI or EnemyCreateMode.LoadSleepNoAI;

    /// <summary>
    /// 是否已经生成过敌人; 预生成模式下为 true 表示不能再生成第二次
    /// </summary>
    public bool HasGenerated => curGenCount > 0;

    public override void OnGameStart()
    {
        if (IsPreLoadMode)
        {
            //进场预生成统一走关卡队列, 间隔由 LevelActionChecker.enemyGenInterval / enemyGroupGenInterval 决定,
            //避免所有预加载敌人都在同一个 delayGen 时间点一起创建导致卡顿
            EnemySpawnQueue.Enqueue(this, TargetRole, GetComponentInParent<EnemyGroupComponent>(true), true, delayGen);
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
        //队列驱动的生成: 谁先到谁生成, 已经生成过的那一份只做"激活AI"
        if (curGenCount <= 0)
        {
            GenEnemy();
            return;
        }

        if (IsPreLoadMode && createMode == EnemyCreateMode.LoadNoAI)
        {
            ActiveEnemyAI();
        }
    }

    //需要一个生成敌人预制体
    [Button("生成/激活已生成AI", enabledMode: EButtonEnableMode.Playmode)]
    public void Gen()
    {
        if (!IsPreLoadMode)
        {
            //AI / NoAI: 走近触发时才真正生成
            if (curGenCount <= 0)
            {
                GenEnemy();
            }

            return;
        }

        if (curGenCount <= 0)
        {
            //预生成队列还没轮到它(玩家跑得比生成快): 补一次生成请求, 走队列保持间隔
            //标记一下, 生成出来时要直接带AI(触发已经发生过了)
            _activateAiOnGen = createMode == EnemyCreateMode.LoadNoAI;
            EnemySpawnQueue.Enqueue(this, TargetRole, GetComponentInParent<EnemyGroupComponent>(true));
            return;
        }

        //以下都是"已经生成过"的情况, 一律不能再生成第二份
        if (createMode == EnemyCreateMode.LoadNoAI)
        {
            ActiveEnemyAI();
        }

        //LoadAI: 生成时就是带AI的, 无需处理; LoadSleepNoAI: 被打才激活, 这里也不动
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
                Enemy0 enemy =
                    EnemyMaker.Inst.CreatEnemy(id, LevelSettingHelper.GetEnemyLevel(enemyAddLevel), skinNameSet);

                var genPos = GetGenPosition(posIndex, posCount);

                ShowEffect(genPos);

                enemy.enemyData.movement.MoveToImmediate(genPos);


                enemy.enemyData.movement.LookToDir(transform.forward);

                enemy.IsAiOn = createMode is EnemyCreateMode.AI or EnemyCreateMode.LoadAI;
                if (createMode == EnemyCreateMode.LoadNoAI)
                {
                    enemy.AddTag(RoleTagCommon.EnableAiIfHurt);

                    //触发早于预生成: 生成出来就直接是激活状态
                    if (_activateAiOnGen)
                    {
                        enemy.IsAiOn = true;
                    }
                }
                else if(createMode == EnemyCreateMode.LoadSleepNoAI)
                {
                    
                }

                if (IsPlayerTeam)
                {
                    enemy.SetTeam(XCSetting.PlayerTeam);
                }

                enemy.DeadAct += OnEnemyDead;

                curGenCount++;

                _enemyList.Add(enemy.id);

                enemy.AiControl.targetRole = TargetRole;

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

    public static Enemy0 GenFriend(string roleKey, Vector3 genPos, int level = 10, string id = "E_0")
    {
        Enemy0 enemy = EnemyMaker.Inst.CreatEnemy(id, level, roleKey);
        enemy.SetTeam(XCSetting.PlayerTeam);
        enemy.IsAiOn = true;
        enemy.enemyData.movement.MoveToImmediate(genPos);
        enemy.SetFriend(GameDataCommon.LocalPlayer);
        return enemy;
    }

    void ActiveEnemyAI()
    {
        foreach (var enemyId in _enemyList)
        {
            if (EntityMgr.Inst.FindEntity<Enemy0>(enemyId, out Enemy0 enemy))
            {
                //预生成的敌人创建时场上可能还没有玩家, 激活时补上目标, AI激活当帧就能追
                if (TargetRole != null)
                {
                    enemy.AiControl.targetRole = TargetRole;
                }

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
        if (count > 1)
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


    public List<string> GetShowActKeyList()
    {
        return ShowActKeys.GetShortKeys();
    }

    void OnTestShowActKeyChange()
    {
        if (string.IsNullOrEmpty(taskLines))
        {
            taskLines = $"{testShowActKey}:";
        }
        else
        {
            taskLines += $"\n{testShowActKey}:";
        }
    }
}


public enum EnemyCreateMode
{
    AI,
    LoadNoAI,
    LoadAI,
    NoAI,
    LoadSleepNoAI //受击不会激活
}