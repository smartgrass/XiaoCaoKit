using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

//单向传送门
public class ItemPortal : MonoBehaviour, IMapMsgSender
{
    public float stayTime = 1.2f;
    [XCLabel("传送前播放特效时间")] public float moveDelay = 0.2f;
    public Transform targetPoint;

    public GameObject targetPointEffect;

    public UnityEvent triggerEvent;

    public UnityEvent exitEvent;

    public UnityEvent preMoveEvent;

    private Coroutine _coroutine;

    private bool _isRunning;

    private void OnValidate()
    {
        if (!targetPointEffect && targetPoint)
        {
            var child = targetPoint.GetChild(0);
            if (child)
            {
                targetPointEffect = child.gameObject;
                Debug.Log($"-- auto set child 0");
            }
        }
    }

    private void OnEnable()
    {
        if (targetPointEffect)
        {
            targetPointEffect.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Tags.PLAYER))
        {
            var idRole = other.GetComponent<IdRole>();
            if (idRole)
            {
                if (_isRunning)
                {
                    StopCoroutine(_coroutine);
                }

                //TODO 表现
                triggerEvent?.Invoke();
                _coroutine = StartCoroutine(IEDelayTrigger(idRole));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _isRunning = false;
        exitEvent?.Invoke();
    }


    IEnumerator IEDelayTrigger(IdRole idRole)
    {
        _isRunning = true;
        yield return new WaitForSeconds(stayTime);
        if (!_isRunning)
        {
            yield break;
        }

        var player = idRole.GetEntity() as Player0;
        if (player == null)
        {
            yield break;
        }

        BaseMsg msg = new BaseMsg();
        msg.state = 0;
        msg.numMsg = 0.5f;
        player.ReceiveMsg(EntityMsgType.HideRender, 0, msg);
        //播放动画
        preMoveEvent?.Invoke();

        targetPointEffect?.SetActive(false);
        
        yield return new WaitForSeconds(moveDelay);

        player.Movement.MoveToImmediate(targetPoint.position);
        
        //显示传送特效
        targetPointEffect?.SetActive(true);
    }
}