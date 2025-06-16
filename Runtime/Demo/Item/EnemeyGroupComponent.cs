using cfg;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

public class EnemeyGroupComponent : GameStartMono, IMapMsgSender
{
    public UnityEvent enterEvent;

    public float radus = 8;

    public UnityEvent allKillEvent;

    public string mapMsg;

    private int triggerTimer;

    private int maxTriggerTime;

    private List<EnemyCreator> creators = new List<EnemyCreator>();

    public bool IsPreLoad;

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
        maxTriggerTime = creators.Count;
    }

    private void OnEnemyDeadEvent(EnemyCreator creator)
    {
        triggerTimer++;
        if (triggerTimer >= maxTriggerTime)
        {
            SendMapMsg();
            allKillEvent?.Invoke();
        }
    }

    public void SendMapMsg()
    {
        GameEvent.Send<string>(EGameEvent.MapMsg.Int(), mapMsg);
    }

    private void Update()
    {
        if (GameDataCommon.Current.gameState == GameState.Running)
        {
            if (GameDataCommon.LocalPlayer != null && GameDataCommon.LocalPlayer.isBodyCreated)
            {
                float dis = Vector3.Distance(transform.position, GameDataCommon.LocalPlayer.transform.position);
                if (dis < radus)
                {
                    enabled = false;
                    enterEvent?.Invoke();
                    OnTriggerAct();
                }
            }
        }
    }

    [Button]
    void OnTriggerAct()
    {
        foreach (var creator in creators)
        {
            creator.Execute();
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radus);
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
