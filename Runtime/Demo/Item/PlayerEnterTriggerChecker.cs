using UnityEngine;
using UnityEngine.Events;
using XiaoCao;

/// <summary>
/// 进入范围触发事件
/// </summary>
public class PlayerEnterTriggerChecker : GameStartMono
{
    public UnityEvent enterEvent;

    public EPlayerEnterEvent playerEnterType;

    public float radus = 8;

    private void Update()
    {
        if (GameDataCommon.Current.gameState == GameState.Running)
        {
            if (GameDataCommon.LocalPlayer != null)
            {
                float dis = Vector3.Distance(transform.position, GameDataCommon.LocalPlayer.transform.position);
                if (dis < radus)
                {
                    enabled = false;
                    enterEvent?.Invoke();
                    DoEvnet();
                }
            }
        }
    }

    void DoEvnet()
    {
        if (playerEnterType == EPlayerEnterEvent.DoExecuteInChhildren)
        {
            ExecuteHelper.DoExecuteInChildren(transform);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radus);
    }


    public enum EPlayerEnterEvent
    {
        None,
        DoExecuteInChhildren
    }
}
