using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// 通道连线收集器: 每个元素是一条通道的两个端点空节点.
/// 节点内容由 Test_DungeonLayout 生成地图时填充, 序列化后可在 Inspector 里直接查看/定位.
/// <para>连通关系只靠这里记录的数据, 运行时不再做射线检测:
/// 同一个 PassLine 的相邻端点 = 通道本身; 同一个 <see cref="PassRoom"/> 内的端点 = 房间内部.</para>
/// </summary>
public class PassPointCollector : MonoBehaviour
{
    [Label("通道连线")]
    [Tooltip("每条通道一条: 两端的空节点, 相邻点必定连通")]
    public List<PassLine> lines = new List<PassLine>();

    [Label("房间分组")]
    [Tooltip("同一个房间里的通道端点: 房间内部互通, 不做遮挡检测")]
    public List<PassRoom> rooms = new List<PassRoom>();
}

/// <summary>一条通道的连线: 按通道走向排列的端点空节点(楼梯通道是 低端 -> 上坡端)</summary>
[System.Serializable]
public class PassLine
{
    [Label("端点")]
    [Tooltip("通道两端的空节点, 顺序即通道走向")]
    public List<Transform> points = new List<Transform>();
}

/// <summary>
/// 一个房间内的通道端点集合(生成地图时按"通道端面贴着哪个房间"登记).
/// 同一房间里的门洞之间没有墙, 所以直接两两连通, 运行时不需要再打射线判断。
/// </summary>
[System.Serializable]
public class PassRoom
{
    [Label("房间")]
    public Transform roomNode;

    [Label("通道端点")]
    public List<Transform> points = new List<Transform>();
}
