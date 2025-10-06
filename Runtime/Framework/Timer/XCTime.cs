using Cysharp.Threading.Tasks;
using DG.Tweening;
using Microsoft.Win32;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using XiaoCao;
using Object = UnityEngine.Object;

public class XCTime
{
    ///<see UniTask示例 cref="Example"/>
    /// <summary>
    /// 对时间都做一层封装,方便处理
    /// </summary>
    internal static float deltaTime => Time.deltaTime * timeScale;

    internal static float fixedDeltaTime => Time.fixedDeltaTime * timeScale;

    internal static float unscalefixedDeltaTime => Time.fixedDeltaTime;
    internal static float unscaledDeltaTime => Time.unscaledDeltaTime;

    public static float timeScale = 1;

    public static Timer DelayTimer(float time, TimerCompleteHandler TimerCompleteHandler)
    {
        var timer = TimerManager.Inst.Register(time, TimerCompleteHandler);
        //timer.Pause();
        //timer.onResume 
        //timer.onDestroy
        //timer.onPause
        return timer;
    }


    //task.Start(); 启动
    //task.Dispose(); 停止
    public static async Task DelayTask(float time, Action action)
    {
        await Task.Delay((int)time * 1000);
        action?.Invoke();
    }


    //UniTask的取消需要依赖 CancellationTokenSource.Cancel(); 相对麻烦
    //但基于值类型的UniTask和自定义的 AsyncMethodBuilder 可以实现0GC
    public static async UniTask DelayRun(float time, Action action, CancellationTokenSource cancellationToken = null)
    {
        CancellationToken token = cancellationToken != null ? cancellationToken.Token : default(CancellationToken);

        await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: token);

        action();
    }

    //UniTask取消方法2
    void TestCancel()
    {
        MonoBehaviour mono = GameObject.FindAnyObjectByType<MonoBehaviour>();
        //取消令牌
        var token =  mono.GetCancellationTokenOnDestroy();

    }


    public static async UniTask DelayRun<T>(float time, Action<T> action, T t, Object bindObject = null)
    {
        bool isBind = bindObject != null;
        await UniTask.Delay(TimeSpan.FromSeconds(time));
        if (!isBind || (bindObject != null))
        {
            action(t);
        }
    }


    public static async UniTask LoopRun(float time, CancellationTokenSource cancellation, Action action)
    {
        var cancelToken = cancellation.Token;
        while (true)
        {
            cancelToken.ThrowIfCancellationRequested();
            await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: cancelToken);
            action();
        }
    }

    //https://blog.csdn.net/dzj2021/article/details/134348978
    //https://github.com/Cysharp/UniTask#getting-started
    //https://qiita.com/toRisouP/items/4445b6b9bf00e49eb147
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

    public static async UniTask Example()
    {
        //Loop 每秒执行一次
        int t = 0;
        var cts = new CancellationTokenSource();
        var task = LoopRun(1f, cts, () =>
        {
            t++;
            Debug.Log($"loop {t}");
            if (t == 3)
            {
                //取消循环
                cts.Cancel();
                cts.Dispose();
                //cts.CancelAfter(1000); //1000毫秒以后取消
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

    public static async UniTask Example2()
    {
        //计时随gameObject销毁停止
        GameObject gameObject = new GameObject();
        await UniTask.DelayFrame(1000, cancellationToken: gameObject.GetCancellationTokenOnDestroy());

        //超时取消
        var timeoutToken = new CancellationTokenSource();
        timeoutToken.CancelAfterSlim(TimeSpan.FromSeconds(5)); // 5sec timeout.

        //To use an async lambda
        Action actEvent = null;
        actEvent += UniTask.Action(async () => { await UniTask.Yield(); });
    }
}

//有需要的话
//public class FluxTime
//{
//    internal static float deltaTime => Time.deltaTime;
//    internal static float fixedDeltaTime => Time.fixedDeltaTime;
//}