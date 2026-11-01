using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

/// <summary>边缘格围墙的生成方式(决定用哪套排布逻辑)</summary>
public enum EdgeBlockMode
{
    [InspectorName("墙(按格平铺)")] Tile = 0,
    [InspectorName("栏杆(按距离排布)")] Railing = 1,
    [InspectorName("空(不生成)")] None = 2,
}

/// <summary>
/// 一种"边缘格围墙"类型: 名称 + 生成方式 + 预制体 + 少量放置参数。
/// 想加新类型(翻边/矮台之类)就在 RoomBlockSo.edgeBlockTypes 里加一项、拖个预制体, 生成逻辑不用改。
/// </summary>
[System.Serializable]
public class EdgeBlockType
{
    [Label("名称")] public string label = "新类型";

    [Label("生成方式")] public EdgeBlockMode mode = EdgeBlockMode.Tile;

    [Label("预制体")] public GameObject prefab;

    [Label("缩放倍率")] public float scale = 1f;

    [Label("额外内缩")] public float inset;

    [Label("高度偏移")] public float yOffset;

    [Label("层")] public int layer = -1;

    [Label("生成碰撞盒")] public bool addCollider = true;

    [Label("标记颜色")] public Color markColor = new Color(0.7f, 0.9f, 0.7f, 1f);
}

/// <summary>
/// 房间拼装用的格块预制体(地板/栏杆/柱子/墙面...) + 边缘格围墙的类型表。
/// Test_RoomLayout 和 Test_DungeonLayout 只配这一个配置即可。
/// </summary>
[CreateAssetMenu(menuName = "SO/RoomBlockSo", fileName = "RoomBlockSo")]
public class RoomBlockSo : ScriptableObject
{
    [Label("地板(单格)")] public GameObject floorPrefab;
    [Label("小地块(1/4格)")] public GameObject smallFloorPrefab;
    [Label("柱子")] public GameObject pillarPrefab;
    [Label("楼梯(单格)")] public GameObject stairPrefab;

    [Label("开口门柱")] public GameObject openingPillarPrefab;

    [Label("支撑柱")] public GameObject supportPillarPrefab;

    [Label("是否使用开口门柱")] public bool edgeMiddleUseOpeningPillar;

    [Label("楼梯栏杆底部抬高")] public float stairRailBase = 0.3f;

    [Label("楼梯栏杆起点内缩")] public float stairRailStartInset = 0.6f;

    [InfoBox("列表顺序 = 地图里存的编号(0/1/2...), 不可变: 新类型只能加在末尾, 不要插入或调整已有项的顺序, " +
             "否则老地图对应编号的格子会静默变成另一种类型", EInfoBoxType.Warning)]
    [Label("边缘格围墙类型")] public List<EdgeBlockType> edgeBlockTypes = new List<EdgeBlockType>();

    /// <summary>按编号(就是列表顺序)取类型; 越界返回 null</summary>
    public EdgeBlockType GetEdgeType(int id)
    {
        if (edgeBlockTypes == null || id < 0 || id >= edgeBlockTypes.Count)
        {
            return null;
        }

        return edgeBlockTypes[id];
    }

    /// <summary>第一个用这种生成方式的类型; 没有返回 null(编号对不上时拿它兜底)</summary>
    public EdgeBlockType GetEdgeType(EdgeBlockMode mode)
    {
        if (edgeBlockTypes == null)
        {
            return null;
        }

        foreach (EdgeBlockType type in edgeBlockTypes)
        {
            if (type != null && type.mode == mode)
            {
                return type;
            }
        }

        return null;
    }
}
