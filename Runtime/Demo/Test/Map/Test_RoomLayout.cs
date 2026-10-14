using System.Collections.Generic;
using GG.Extensions;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;
using XiaoCao.Map;

/// <summary>
/// 房间排版工具: 输入房间尺寸(列x 行y), 一键铺地板 / 围栏杆 / 沿边按间距摆柱子, 贴墙的面用墙面代替栏杆.
/// 生成结构与 Assets/_Res/Item/Static/room/FloorRect.prefab 类似:
/// 根节点下是 1个地板节点(Test_MapLayout) + 若干边栏杆节点(Test_WallLayout) + 若干边墙面节点(Test_MapLayout) + 4个边柱子节点(Test_MapLayout),
/// 各部分都留着对应工具的参数, 可以单独在 Inspector 上调整后再点它自己的生成按钮.
/// </summary>
public class Test_RoomLayout : MonoBehaviour
{
    [Label("地板(单格)")] public GameObject floorPrefab;
    [Label("栏杆(单格)")] public GameObject railingPrefab;
    [Label("柱子")] public GameObject pillarPrefab;
    [Label("墙面(单格)")] public GameObject wallPrefab;

    [InfoBox("尺寸: x = 列数(x方向), y = 行数(z方向). 例: (4,3) 表示 4列3行")]
    [Label("房间尺寸(列x, 行y)")] public Vector2Int roomSize = new Vector2Int(4, 3);

    [Label("地板追加间距")] public Vector3 addValue;

    [Label("栏杆缩放")] public float railingScale = 0.7f;
    [Min(1)] [Label("栏杆每组段数")] public int railingBodyCount = 2;
    [Label("栏杆向房间内缩")] public float railingInset;
    [Label("生成墙体碰撞盒")] public bool addWallCollider = true;

    [Min(1f)] [Label("柱子间隔(格)")] public float pillarInterval = 2f;
    [Label("柱子向房间内缩")] public float pillarInset;
    [Label("柱子高度偏移")] public float pillarY;

    [InfoBox("勾选的面用墙面代替栏杆: XMin/XMax = x最小/最大两侧, ZMin/ZMax = z最小/最大两侧")]
    [EnumFlags] [Label("贴墙的面")] public RoomFace wallFaces = RoomFace.None;

    /// <summary>房间的四个面</summary>
    [System.Flags]
    public enum RoomFace
    {
        None = 0,
        XMin = 1,
        XMax = 2,
        ZMin = 4,
        ZMax = 8,
        AllFaces = XMin | XMax | ZMin | ZMax,
    }

#if UNITY_EDITOR
    [Button("生成房间")]
    public void GenRoom()
    {
        if (floorPrefab == null || pillarPrefab == null)
        {
            Debug.LogError("[RoomLayout] 需要先指定 地板/柱子 预制体");
            return;
        }

        int cols = Mathf.Max(1, roomSize.x);
        int rows = Mathf.Max(1, roomSize.y);

        if (wallFaces != RoomFace.None && wallPrefab == null)
        {
            Debug.LogError("[RoomLayout] 勾选了贴墙的面, 需要先指定 墙面 预制体");
            return;
        }

        // 没有贴墙的面才需要围栏杆
        Test_WallCell railingCell = null;
        if ((RoomFace.AllFaces & ~wallFaces) != RoomFace.None)
        {
            railingCell = railingPrefab == null ? null : railingPrefab.GetComponent<Test_WallCell>();
            if (railingCell == null)
            {
                Debug.LogError("[RoomLayout] 有面要围栏杆, 需要指定带 Test_WallCell 的栏杆预制体(参考 HandrailLayoutCell)");
                return;
            }
        }

        // 单格地板尺寸用与 Test_MapLayout 相同的算法, 保证地板节点的生成结果一致
        Bounds floorBox = GetLayoutBox(floorPrefab);
        if (Mathf.Abs(floorBox.size.x) < 0.001f || Mathf.Abs(floorBox.size.z) < 0.001f)
        {
            Debug.LogError($"[RoomLayout] 地板预制体 {floorPrefab.name} 量不出尺寸, 需要带 Mesh 的单格地板");
            return;
        }

        transform.DestroyChildren();

        Transform floorNode = GenFloorNode(rows, cols, floorBox);
        // 房间范围以实际铺出来的地板为准, 避免依赖预制体内部的偏移
        Bounds roomArea = GetLocalRendererBounds(floorNode);
        GenRailings(roomArea, railingCell);
        GenWalls(roomArea);
        GenPillars(roomArea, cols, rows, GetLayoutBox(pillarPrefab));

        Debug.Log($"[RoomLayout] 生成完成: {cols}列 x {rows}行, 贴墙面={wallFaces}, 房间范围 {roomArea.size}");
    }

    [Button("清理房间")]
    public void ClearRoom()
    {
        transform.DestroyChildren();
    }

    /// <summary>铺地板: 生成一个 Test_MapLayout 节点(与 FloorRect 里的地板节点一致)</summary>
    private Transform GenFloorNode(int rows, int cols, Bounds floorBox)
    {
        GameObject node = CreateNode(floorPrefab.name, Vector3.zero, Vector3.zero, Vector3.one);

        Test_MapLayout layout = node.AddComponent<Test_MapLayout>();
        layout.prefab = floorPrefab;
        layout.genXYZ = new Vector3Int(cols, 1, rows); // x列, y层, z行
        layout.addValue = addValue;
        layout.IsHollow = false;
        layout.prefabName = floorPrefab.name;
        layout.boxSize = floorBox.size;
        layout.center = floorBox.center;
        layout.TestGent();

        return node.transform;
    }

    /// <summary>围栏杆: 四条边里没有贴墙的, 各生成一个 Test_WallLayout 节点</summary>
    private void GenRailings(Bounds area, Test_WallCell cell)
    {
        float y = area.max.y;
        // 沿x的两条边, 栏杆沿节点本地+x排列
        if (!HasWallFace(RoomFace.ZMin)) GenRailingRun(new Vector3(area.min.x, y, area.min.z), 0f, area.size.x, Vector3.forward, 0, cell);
        if (!HasWallFace(RoomFace.ZMax)) GenRailingRun(new Vector3(area.min.x, y, area.max.z), 0f, area.size.x, Vector3.back, 1, cell);

        // 沿z的两条边, 节点绕Y轴-90度后本地+x指向房间+z方向
        if (!HasWallFace(RoomFace.XMin)) GenRailingRun(new Vector3(area.min.x, y, area.min.z), -90f, area.size.z, Vector3.right, 2, cell);
        if (!HasWallFace(RoomFace.XMax)) GenRailingRun(new Vector3(area.max.x, y, area.min.z), -90f, area.size.z, Vector3.left, 3, cell);
    }

    /// <summary>该面是否要贴墙</summary>
    private bool HasWallFace(RoomFace face)
    {
        return (wallFaces & face) != RoomFace.None;
    }

    /// <summary>贴墙: 勾选的面各生成一个 Test_MapLayout 节点, 沿该面排满墙面</summary>
    private void GenWalls(Bounds area)
    {
        if (wallFaces == RoomFace.None)
        {
            return;
        }

        Bounds wallBox = GetLayoutBox(wallPrefab);
        float y = area.max.y;

        // 沿x的两条边, 墙面沿节点本地+x排开
        if (HasWallFace(RoomFace.ZMin)) GenWallRun(new Vector3(area.min.x, y, area.min.z), 0f, area.size.x, 0, wallBox);
        if (HasWallFace(RoomFace.ZMax)) GenWallRun(new Vector3(area.min.x, y, area.max.z), 0f, area.size.x, 1, wallBox);

        // 沿z的两条边, 节点绕Y轴-90度后本地+x指向房间+z方向
        if (HasWallFace(RoomFace.XMin)) GenWallRun(new Vector3(area.min.x, y, area.min.z), -90f, area.size.z, 2, wallBox);
        if (HasWallFace(RoomFace.XMax)) GenWallRun(new Vector3(area.max.x, y, area.min.z), -90f, area.size.z, 3, wallBox);
    }

    /// <summary>生成一面墙: 墙段沿节点本地+x排开(yaw控制墙面朝向), 按边的长度均分, 保证两端对齐</summary>
    private void GenWallRun(Vector3 startPos, float yaw, float runLength, int index, Bounds wallBox)
    {
        int count = Mathf.Max(1, Mathf.RoundToInt(runLength / Mathf.Max(0.001f, Mathf.Abs(wallBox.size.x))));
        float step = runLength / count;

        GameObject node = CreateNode(IndexedName(wallPrefab.name, index), startPos, new Vector3(0f, yaw, 0f), Vector3.one);
        node.layer = Layers.WALL_BLOCK;

        Test_MapLayout layout = node.AddComponent<Test_MapLayout>();
        layout.prefab = wallPrefab;
        layout.genXYZ = new Vector3Int(count, 1, 1);
        layout.addValue = new Vector3(step - wallBox.size.x, 0f, 0f);
        layout.IsHollow = false;
        layout.prefabName = wallPrefab.name;
        layout.boxSize = wallBox.size;
        layout.center = wallBox.center;
        layout.TestGent();

        if (addWallCollider)
        {
            BuildingTool.AddBoxCollider(node.transform);
        }
    }

    /// <summary>生成一条边上的栏杆, runLength为这条边的世界长度</summary>
    private void GenRailingRun(Vector3 startPos, float yaw, float runLength, Vector3 inwardDir, int index, Test_WallCell cell)
    {
        Vector3 pos = startPos + inwardDir * railingInset;
        GameObject node = CreateNode(IndexedName(railingPrefab.name.Replace("Cell", ""), index), pos,
            new Vector3(0f, yaw, 0f), Vector3.one * railingScale);

        Test_WallLayout layout = node.AddComponent<Test_WallLayout>();
        layout.prefab = cell;
        layout.bodyCount = Mathf.Max(1, railingBodyCount);
        layout.len = CalcRailingLen(runLength, cell, layout.bodyCount);
        layout.Gen();

        if (addWallCollider)
        {
            BuildingTool.AddBoxCollider(node.transform);
        }
    }

    /// <summary>按 每组(bodyCount个body + 1个node) 的长度, 算出铺满这条边需要多少段body</summary>
    private int CalcRailingLen(float runLength, Test_WallCell cell, int bodyCount)
    {
        float runLocal = Mathf.Abs(runLength) / Mathf.Max(0.0001f, railingScale);
        // 1段body占 bodyWidth, 每 bodyCount 段body后面跟1个node, 折算成每段body平均占的长度
        float lengthPerBody = cell.bodyWidth + cell.nodeWidth / bodyCount;
        return Mathf.Max(1, Mathf.RoundToInt(runLocal / Mathf.Max(0.0001f, lengthPerBody)));
    }

    /// <summary>取预制体在排版空间里的单格尺寸(与 Test_MapLayout 的"计算"一致, 会把预制体自身的旋转算进去)</summary>
    private static Bounds GetLayoutBox(GameObject prefab)
    {
        Bounds bounds = BuildingTool.GetPrefabBounds(prefab);
        if (!prefab.transform.localEulerAngles.IsZore())
        {
            bounds.size = prefab.transform.rotation * bounds.size;
            bounds.center = prefab.transform.rotation * bounds.center;
        }

        return bounds;
    }

    /// <summary>沿四条边摆柱子: 每条边一个 Test_MapLayout 节点, 四个角落只摆一次</summary>
    private void GenPillars(Bounds area, int cols, int rows, Bounds pillarBox)
    {
        float y = area.max.y + pillarY;

        // 按格数算区间数, 再按边长均匀分配, 保证两端角落都有柱子且间距不超过设定间隔
        int segmentsX = CalcPillarSegments(cols, pillarInterval);
        int segmentsZ = CalcPillarSegments(rows, pillarInterval);
        int countX = segmentsX + 1;
        int countZ = segmentsZ + 1;
        float stepX = area.size.x / segmentsX;
        float stepZ = area.size.z / segmentsZ;

        Vector3Int genX = new Vector3Int(countX, 1, 1);
        Vector3 spacingX = new Vector3(stepX - pillarBox.size.x, 0f, 0f);
        GenPillarRun(new Vector3(area.min.x, y, area.min.z + pillarInset), genX, spacingX, pillarBox, 0);
        GenPillarRun(new Vector3(area.min.x, y, area.max.z - pillarInset), genX, spacingX, pillarBox, 1);

        // 沿z的两条边跳过两端角落, 角落由上面的两条边负责
        if (countZ > 2)
        {
            Vector3Int genZ = new Vector3Int(1, 1, countZ - 2);
            Vector3 spacingZ = new Vector3(0f, 0f, stepZ - pillarBox.size.z);
            GenPillarRun(new Vector3(area.min.x + pillarInset, y, area.min.z + stepZ), genZ, spacingZ, pillarBox, 2);
            GenPillarRun(new Vector3(area.max.x - pillarInset, y, area.min.z + stepZ), genZ, spacingZ, pillarBox, 3);
        }
    }

    /// <summary>生成一条边上的一排柱子, spacingValue用于把Test_MapLayout的间距调整成stepX/stepZ</summary>
    private void GenPillarRun(Vector3 startPos, Vector3Int genXYZ, Vector3 spacingValue, Bounds pillarBox, int index)
    {
        GameObject node = CreateNode(IndexedName(pillarPrefab.name + "_Layout", index), startPos, Vector3.zero, Vector3.one);

        Test_MapLayout layout = node.AddComponent<Test_MapLayout>();
        layout.prefab = pillarPrefab;
        layout.genXYZ = genXYZ;
        layout.addValue = spacingValue;
        layout.IsHollow = false;
        layout.prefabName = pillarPrefab.name;
        layout.boxSize = pillarBox.size;
        layout.center = pillarBox.center;
        layout.TestGent();
    }

    /// <summary>柱子的区间数: 间隔以格为单位, 留一点浮点余量, 保证实际间距不超过设定间隔</summary>
    private static int CalcPillarSegments(int cellCount, float intervalCells)
    {
        return Mathf.Max(1, Mathf.CeilToInt(cellCount / Mathf.Max(0.001f, intervalCells) - 0.001f));
    }

    /// <summary>取节点(含子物体)在自身局部空间的包围盒</summary>
    private static Bounds GetLocalRendererBounds(Transform node)
    {
        Matrix4x4 toLocal = node.worldToLocalMatrix;
        bool hasBounds = false;
        Bounds result = new Bounds();

        foreach (Renderer renderer in node.GetComponentsInChildren<Renderer>(true))
        {
            Vector3 center = toLocal.MultiplyPoint3x4(renderer.bounds.center);
            Vector3 size = toLocal.MultiplyVector(renderer.bounds.size);
            Bounds bounds = new Bounds(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)));

            if (!hasBounds)
            {
                result = bounds;
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(bounds);
            }
        }

        return result;
    }

    /// <summary>新建子节点并设置本地变换</summary>
    private GameObject CreateNode(string name, Vector3 localPos, Vector3 localEuler, Vector3 localScale)
    {
        GameObject node = new GameObject(name);
        node.transform.SetParent(transform, false);
        node.transform.localPosition = localPos;
        node.transform.localEulerAngles = localEuler;
        node.transform.localScale = localScale;
        return node;
    }

    /// <summary>子节点命名沿用 名字+(序号) 的写法</summary>
    private static string IndexedName(string baseName, int index)
    {
        return index == 0 ? baseName : $"{baseName} ({index})";
    }
#endif
}
