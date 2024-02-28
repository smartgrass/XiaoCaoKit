using NaughtyAttributes;
using TEngine;
using UnityEngine;
using XiaoCao;
using EventType = XiaoCao.EventType;
/// <summary>
/// Editor test
/// </summary>
/// 
public class GameStartMono: MonoBehaviour
{
    /// <summary>
    /// Mono执行start时, GameState可能还处于加载中, 所以需要等待加载完成
    /// </summary>
    [ReadOnly]
    public bool isGameStarted;

    public virtual void Start()
    {
        if (GameDataCommon.Current.gameState == GameState.Running)
        {
            OnGameStart();
        }
        else
        {
            GameEvent.AddEventListener(EventType.GameStartFinsh.Int(), OnGameStart);
        }
    }

    public virtual void OnDestroy()
    {
        GameEvent.RemoveEventListener(EventType.GameStartFinsh.Int(), OnGameStart);
    }

    public virtual void OnGameStart()
    {
        isGameStarted = true;
    }
}
