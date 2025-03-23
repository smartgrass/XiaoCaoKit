using NaughtyAttributes;
using System;
using TEngine;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

public class EnemeyGroupComponent : GameStartMono
{
    public UnityEvent enterEvent;

    public float radus = 8;

    public UnityEvent allKillEvent;

    public string mapMsg;

    private int triggerTimer;

    private int maxTriggerTime;

    private EnemyCreator[] creators;

    private void Awake()
    {
        creators = transform.GetComponentsInChildren<EnemyCreator>();
        foreach (var creator in creators)
        {
            creator.enemyAllDead += OnEnemyDeadEvent;
            creator.gameObject.SetActive(false);
        }
        maxTriggerTime = creators.Length;
    }

    private void OnEnemyDeadEvent(EnemyCreator creator)
    {
        triggerTimer++;
        if (triggerTimer >= maxTriggerTime)
        {
            GameEvent.Send<string>(EGameEvent.MapMsg.Int(), mapMsg);
            allKillEvent?.Invoke();
        }
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


    //GameEvent.RemoveEventListener<string>(EGameEvent.EnemyGroupEndEvent.Int(), OnEnemyDeadEvent);
}
