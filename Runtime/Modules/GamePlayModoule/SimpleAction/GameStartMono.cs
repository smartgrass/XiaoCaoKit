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
    public void Start()
    {
        GameEvent.AddEventListener(EventType.GameStartFinsh.Int(), OnGameStart);
    }

    private void OnDestroy()
    {
        GameEvent.RemoveEventListener(EventType.GameStartFinsh.Int(), OnGameStart);
    }

    public virtual void OnGameStart()
    {

    }
}
