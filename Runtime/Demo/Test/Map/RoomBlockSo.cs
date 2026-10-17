using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// 房间拼装用的四种格块预制体(地板/栏杆/柱子/墙面).
/// Test_RoomLayout 和 Test_DungeonLayout 只配这一个配置即可.
/// </summary>
[CreateAssetMenu(menuName = "SO/RoomBlockSo", fileName = "RoomBlockSo")]
public class RoomBlockSo : ScriptableObject
{
    [Label("地板(单格)")] public GameObject floorPrefab;
    [InfoBox("地板尺寸带 0.5 格时, 差的那截用它补(一般是整格的 1/4, 比如 Floor_4_of_1)")]
    [Label("小地块(1/4格)")] public GameObject smallFloorPrefab;
    [Label("栏杆(单格)")] public GameObject railingPrefab;
    [Label("柱子")] public GameObject pillarPrefab;
    [Label("墙面(单格)")] public GameObject wallPrefab;
    [Label("楼梯(单格)")] public GameObject stairPrefab;

    [InfoBox("开口(门洞)两侧补的柱子用它, 一般比普通柱子小一号")]
    [Label("开口门柱")] public GameObject openingPillarPrefab;

    [InfoBox("勾选后: 四个边缘角仍然是普通柱子, 每条边中间的柱子换成开口门柱")]
    [Label("是否使用开口门柱")] public bool edgeMiddleUseOpeningPillar;

    [InfoBox("楼梯预制体踏步面相对它自身原点的高度(Step_A 是 0.3), 栏杆/墙的底部会抬这么多, 正好贴在踏步面上")]
    [Label("楼梯栏杆底部抬高")] public float stairRailBase = 0.3f;

    [InfoBox("楼梯斜栏杆的起点从低端往坡上缩这么多(只收低端那一头, 高端端头不动), 用来消掉低端探出去的那一小截栏杆")]
    [Label("楼梯栏杆起点内缩")] public float stairRailStartInset = 0.6f;
}
