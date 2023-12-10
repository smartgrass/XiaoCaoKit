using UnityEngine;

public class FluxTime
{
    /// <summary>
    /// 对时间都做一层封装,方便处理
    /// </summary>
    internal static float deltaTime => Time.deltaTime;
    internal static float fixedDeltaTime => Time.fixedDeltaTime;
}