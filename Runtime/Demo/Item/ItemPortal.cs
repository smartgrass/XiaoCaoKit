using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

//单向传送门
public class ItemPortal : MonoBehaviour, IMapMsgSender
{
    public float stayTime = 1.2f;

    public Transform targetPoint;

    public UnityEvent triggerEvent;

    public UnityEvent exitEvent;

    public UnityEvent triggerSucceeEvent;

    private Coroutine coroutine;

    private bool isRunning;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Tags.PLAYER))
        {
            var idRole = other.GetComponent<IdRole>();
            if (idRole)
            {
                if (isRunning)
                {
                    StopCoroutine(coroutine);
                }
                //TODO 表现
                triggerEvent?.Invoke();
                coroutine = StartCoroutine(IEDelayTrigger(idRole));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        isRunning = false;
        exitEvent?.Invoke();
    }

    IEnumerator IEDelayTrigger(IdRole idRole)
    {
        isRunning = true;
        yield return new WaitForSeconds(stayTime);
        if (!isRunning)
        {
            yield break;
        }
        var player = idRole.GetEntity() as Player0;
        player.Movement.MoveToImmediate(targetPoint.position);
        isRunning = false;
        triggerSucceeEvent?.Invoke();
    }

}
