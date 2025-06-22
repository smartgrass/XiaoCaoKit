using NaughtyAttributes;
using TEngine;
using UnityEngine;
using XiaoCao;
using EGameEvent = XiaoCao.EGameEvent;
/// <summary>
/// 用于获取 GameStart回调, 控制时序
/// </summary>
public class GameStartMono : MonoBehaviour
{
    [ReadOnly]
    public bool isGameStarted;

    private bool hasAddListener;

    public virtual void Start()
    {
        if (GameDataCommon.Current.gameState == GameState.Running)
        {
            OnGameStart();
        }
        else
        {
            hasAddListener = true;
            GameEvent.AddEventListener(EGameEvent.GameStartFinsh.Int(), OnGameStart);
        }
    }

    public void OnDestroy()
    {
        RemoveListener();
    }


    public virtual void RemoveListener()
    {
        if (hasAddListener)
        {
            GameEvent.RemoveEventListener(EGameEvent.GameStartFinsh.Int(), OnGameStart);
        }
    }

    public virtual void OnGameStart()
    {
        isGameStarted = true;
        //表示玩家已经加载完成
    }
}
