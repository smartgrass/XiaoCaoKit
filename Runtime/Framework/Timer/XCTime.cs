//https://github.com/Cysharp/UniTask#getting-started

/*
await UniTask.WaitUntil(() => isActive == false);
await UniTask.Delay(TimeSpan.FromSeconds(10), ignoreTimeScale: false);
async UniTask<Sprite> LoadAsSprite(string path)
{
    var resource = await Resources.LoadAsync<Sprite>(path);
    return (resource as Sprite);
}


await UniTask.WhenAll(task1, task2);
await UniTask.WhenAny(task1, task2);  //任意一个完成

//简单延时操作
 async UniTask ExampleDelay()
{
    await UniTask.Delay(TimeSpan.FromSeconds(1));
    Debug.Log("Delayed log message.");
}
 */
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class XCTime
{
    /// <summary>
    /// 对时间都做一层封装,方便处理
    /// </summary>
    internal static float deltaTime => Time.deltaTime;
    internal static float fixedDeltaTime => Time.fixedDeltaTime;
    internal static float unscaledDeltaTime => Time.unscaledDeltaTime;



    /// <param name="bindObject">当bindObject被销毁则不执行 </param>
    /// <returns></returns>
    public static async UniTask DelayRun(float time, Action action)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(time));
        action();
    }
    public static async UniTask DelayRun<T>(float time, Action<T> action, T t,Object bindObject = null)
    {
        bool isBind = bindObject != null;
        await UniTask.Delay(TimeSpan.FromSeconds(time));
        if (!isBind || (bindObject != null))
        {
            action(t);
        }
    }
}