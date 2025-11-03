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

    public string mapMsg;

    private List<EnemyCreator> creators = new List<EnemyCreator>();

    public bool isPreLoad;

    public string popToastKey;

    private int _triggerTimer;
    private int _maxTriggerTime;
    private float _lastCheckTime;
    private float _maxHighDelta = 2; //高度差超过这个值则不触发
    public Vector3 triggerPos;
    public Role TargetRole { get; set; }

    private void Awake()
    {
        //有顺序要求
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.TryGetComponent<EnemyCreator>(out EnemyCreator creator))
            {
                creators.Add(creator);
            }
        }


        foreach (var creator in creators)
        {
            creator.enemyAllDead += OnEnemyDeadEvent;
            //creator.gameObject.SetActive(false);
        }

        _maxTriggerTime = creators.Count;
    }

    private void OnEnemyDeadEvent(EnemyCreator creator)
    {
        _triggerTimer++;
        if (_triggerTimer >= _maxTriggerTime)
        {
            SendMapMsg();
            allKillEvent?.Invoke();
        }
    }

    public void SendMapMsg()
    {
        GameEvent.Send<string>(EGameEvent.MapMsg.Int(), mapMsg);
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
        foreach (var creator in creators)
        {
            creator.TargetRole = TargetRole;
            creator.Execute();
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
        var group = LubanTables.GetCreateEnemyGroups(key);
        for (int i = 0; i < creators.Count; i++)
        {
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
        }
    }
}