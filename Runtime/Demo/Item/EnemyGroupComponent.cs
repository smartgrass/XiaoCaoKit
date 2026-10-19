using cfg;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;
using XiaoCao.UI;

public class EnemyGroupComponent : GameStartMono, IMapMsgSender
{
    public static EnemyGroupComponent Current;

    public UnityEvent enterEvent;

    public float radus = 8;

    public UnityEvent allKillEvent;

    //group完成时发送的消息key, 恒取节点名; 不序列化, 避免节点改名后读到旧缓存值
    [ShowNativeProperty]
    public string MapMsg => gameObject.name;

    //group完成时激活的节点名前缀
    public const string ActiveOnEndPrefix = "End_";

    //敌人组节点名前缀
    public const string GroupPrefix = "Group";

    //子节点等级覆写配置的前缀, 完整格式 addLevel:1
    private const string AddLevelPrefix = "addLevel:";

    //勾选后group完成时自动激活同级的下一个Group
    public bool activeNextGroup;

    //group完成时激活的同级节点名, 恒为 End_ + 节点名; 不序列化
    [ShowNativeProperty]
    public string ActiveOnEnd => ActiveOnEndPrefix + MapMsg;

    private List<EnemyCreator> creators = new List<EnemyCreator>();

    public bool isPreLoad;

    public string popToastKey;

    private int _triggerTimer;
    private int _maxTriggerTime;
    private float _lastCheckTime;
    private float _maxHighDelta = 2; //高度差超过这个值则不触发
    public Vector3 triggerPos;
    public Role TargetRole { get; set; }

    public string GetSendMapMsg => MapMsg;

    private void Awake()
    {
        CheckChildGroup();

        foreach (var creator in creators)
        {
            creator.enemyAllDead += OnEnemyDeadEvent;
            //creator.gameObject.SetActive(false);
        }

        _maxTriggerTime = creators.Count;
    }

    private void CheckChildGroup()
    {
        if (creators.Count != 0) return;
        //有顺序要求
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.TryGetComponent<EnemyCreator>(out EnemyCreator creator))
            {
                creators.Add(creator);
            }
        }
    }

    private void OnEnemyDeadEvent(EnemyCreator creator)
    {
        _triggerTimer++;
        if (_triggerTimer >= _maxTriggerTime)
        {
            OnGroupFinish();
        }
    }

    /// <summary>
    /// group完成: 发送消息 + 激活指定的后续节点
    /// </summary>
    private void OnGroupFinish()
    {
        SendMapMsg();
        ActiveEndObject();
        ActiveNextGroup();
        allKillEvent?.Invoke();
        GameEvent.Send(EGameEvent.EnemyGroupEndEvent.ToInt(), _maxTriggerTime, MapMsg);
    }

    /// <summary>
    /// 激活同级的下一个Group节点, 用于按顺序串起多波敌人
    /// </summary>
    private void ActiveNextGroup()
    {
        if (!activeNextGroup || !transform.parent)
        {
            return;
        }

        //从自身往后找第一个Group节点, 跳过End_等非Group节点
        Transform parentTf = transform.parent;
        for (int i = transform.GetSiblingIndex() + 1; i < parentTf.childCount; i++)
        {
            Transform nextTf = parentTf.GetChild(i);
            if (nextTf.name.StartsWith(GroupPrefix))
            {
                nextTf.gameObject.SetActive(true);
                return;
            }
        }
    }

    /// <summary>
    /// 激活同级节点下名为 ActiveOnEnd 的物体, 用于group完成后开启后续内容
    /// </summary>
    private void ActiveEndObject()
    {
        Transform parentTf = transform.parent;
        if (!parentTf)
        {
            return;
        }

        Transform endTf = parentTf.Find(ActiveOnEnd);
        if (!endTf)
        {
            Debuger.Log($"--- no end object {ActiveOnEnd} in {parentTf.name}");
            return;
        }

        endTf.gameObject.SetActive(true);
    }

    public void SendMapMsg()
    {
        GameEvent.Send<string>(EGameEvent.MapMsg.ToInt(), MapMsg);
        UIMgr.PopToastKey(popToastKey);
    }

    private void Update()
    {
        if (radus <= 0)
        {
            return;
        }

        //每隔0.5s检测一次
        if (Time.timeSinceLevelLoad - _lastCheckTime < 0.5f)
        {
            return;
        }

        _lastCheckTime = Time.timeSinceLevelLoad;

        if (GameDataCommon.Current.gameState != GameState.Running) return;
        if (GameDataCommon.LocalPlayer == null || !GameDataCommon.LocalPlayer.isBodyCreated) return;

        TargetRole = GameDataCommon.LocalPlayer;
        Transform localPlayerTf = TargetRole.transform;
        triggerPos = localPlayerTf.position;
        float dis = Vector3.Distance(transform.position, localPlayerTf.position);
        if (dis < radus)
        {
            //高度差超过这个值则不触发
            float heightDelta = Math.Abs(transform.position.y - localPlayerTf.position.y);
            if (heightDelta > _maxHighDelta)
            {
                return;
            }

            enabled = false;
            enterEvent?.Invoke();
            Current = this;
            OnTriggerAct();
        }
    }

    [Button]
    void OnTriggerAct()
    {
        //统一走关卡队列, 间隔由 LevelActionChecker.enemyGenInterval / enemyGroupGenInterval 在初始化时注入
        //传入 this 作为分组标记: 换到下个组时队列会额外等待 enemyGroupGenInterval
        foreach (var creator in creators)
        {
            EnemySpawnQueue.Enqueue(creator, TargetRole, this);
        }
    }

    private void OnDrawGizmos()
    {
        if (enabled)
        {
            Gizmos.DrawWireSphere(transform.position, radus);
        }
    }


    public void GetCreateEnemyInfoFromConfig(string key)
    {
        CheckChildGroup();
        var group = LubanTables.GetCreateEnemyGroups(key);

        //配置缺失时保留场景上的默认设置, 不要抛异常中断地图上后续组的配置(查不到时LubanTables已打错误日志)
        if (group == null)
        {
            return;
        }

        bool hasTaskLines = !string.IsNullOrEmpty(group.TaskLines);

        //多行配置里留空的行会被跳过, 数量对不上说明填漏了, 会导致后面的等级错位到前面的子节点
        if (group.AddEnemyInfos.Count > 0 && group.AddEnemyInfos.Count != creators.Count)
        {
            Debuger.LogWarning($"--- {key} addEnemyInfos 行数 {group.AddEnemyInfos.Count} 与子节点数 {creators.Count} 不一致");
        }

        for (int i = 0; i < creators.Count; i++)
        {
            if (hasTaskLines)
            {
                creators[i].taskLines = group.TaskLines;
            }

            if (i < group.EnemyInfos.Count)
            {
                creators[i].enemyIdList = group.EnemyInfos[i].Enemys;
                creators[i].genCount = group.EnemyInfos[i].Count;
            }
            else
            {
                creators[i].enemyIdList = group.EnemyInfos[0].Enemys;
                creators[i].genCount = group.EnemyInfos[0].Count;
            }

            //按子节点顺序覆写等级偏移, 未配置或格式不符时保留场景上的 enemyAddLevel
            if (i < group.AddEnemyInfos.Count)
            {
                int addLevel = ParseAddLevel(group.AddEnemyInfos[i]);
                if (addLevel >= 0)
                {
                    creators[i].enemyAddLevel = addLevel;
                }
            }
        }
    }

    /// <summary>
    /// 解析子节点等级覆写配置(addLevel:1), 格式不符返回 -1 表示不覆写
    /// </summary>
    private static int ParseAddLevel(string addEnemyInfo)
    {
        if (!addEnemyInfo.StartsWith(AddLevelPrefix))
        {
            return -1;
        }

        string value = addEnemyInfo.Substring(AddLevelPrefix.Length);

        return int.TryParse(value, out int addLevel) ? addLevel : -1;
    }
}
