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
 */
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

public class XCTime
{
    public static async UniTask Example()
    {
        //Loop 每秒执行一次
        int t = 0;
        var cancellationToken = new CancellationTokenSource();
        var task = LoopRun(1f, cancellationToken, () => {
            t++;
            Debug.Log($"loop {t}");
            if (t == 3)
            {
                //取消循环
                cancellationToken.Cancel();
            }
        });
        await UniTask.Delay(TimeSpan.FromSeconds(3));

        //等待1s
        await UniTask.Delay(TimeSpan.FromSeconds(1), ignoreTimeScale: false);

        //等待Mono生命周期触发（PreUpdate、Update、LateUpdate 等...
        await UniTask.Yield(PlayerLoopTiming.PreLateUpdate);

        //方法内取消
        throw new OperationCanceledException();
    }

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



    public static async UniTask LoopRun(float time, CancellationTokenSource cancellation ,Action action)
    {
        var cancelToken = cancellation.Token;
        while (true)
        {
            cancelToken.ThrowIfCancellationRequested();
            await UniTask.Delay(TimeSpan.FromSeconds(time),cancellationToken: cancelToken);
            action();
        }
    }
}