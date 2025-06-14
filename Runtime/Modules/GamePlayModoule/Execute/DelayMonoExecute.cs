using Cysharp.Threading.Tasks;
using UnityEngine;
using XiaoCao;

/// <summary>
/// 延迟执行-抽象类
/// </summary>
public abstract class DelayMonoExecute : MonoExecute
{
    public float delay = 0;
    public override void Execute()
    {
        XCTime.DelayRun(delay, AterDelay).Forget();
    }

    public void AterDelay()
    {
        if (!gameObject)
        {
            Debuger.LogError($"--- game destory {this}");
            return;
        }
        ExecuteOnTime();
    }

    public virtual void ExecuteOnTime()
    {
        throw new System.Exception("ExecuteOnTime null");
    }
}
