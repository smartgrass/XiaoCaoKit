using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// 通道连线收集器: 每个元素是一条通道的两个端点空节点.
/// 节点内容由 Test_DungeonLayout 生成地图时填充, 序列化后可在 Inspector 里直接查看/定位.
/// </summary>
public class PassPointCollector : MonoBehaviour
{
    [Label("通道连线")]
    public List<PassLine> lines = new List<PassLine>();
}

/// <summary>一条通道的连线: 按通道走向排列的端点空节点(楼梯通道是 低端 -> 上坡端)</summary>
[System.Serializable]
public class PassLine
{
    [Label("端点")]
    [Tooltip("通道两端的空节点, 顺序即通道走向")]
    public List<Transform> points = new List<Transform>();
}
