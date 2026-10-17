using System;
using System.Collections.Generic;
using GG.Extensions;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;
using XiaoCao.Map;

/// <summary>通道的楼梯上坡方向(按画布方向: 上=ZMax, 下=ZMin, 左=XMin, 右=XMax)</summary>
public enum StairDir
{
    [InspectorName("无")] None,
    [InspectorName("上")] Up,
    [InspectorName("下")] Down,
    [InspectorName("左")] Left,
    [InspectorName("右")] Right
}

/// <summary>格坐标上的一个房间或通道(行列都以格为单位)</summary>
[Serializable]
public class DungeonPiece
{
    public string name;
    /// <summary>格坐标(列): 支持 0.5 格</summary>
    public float col;
    /// <summary>格坐标(行, 从上往下数): 支持 0.5 格</summary>
    public float row;
    /// <summary>宽(列): 支持 0.5 格</summary>
    public float cols = 3;
    /// <summary>高(行): 支持 0.5 格</summary>
    public float rows = 3;
    public bool isCorridor;
    [Label("楼梯方向")] public StairDir stair = StairDir.None;
    [Label("哪几面有墙")] public Test_RoomLayout.RoomFace wallFaces = Test_RoomLayout.RoomFace.AllFaces;
    [Label("边缘格围墙(面 + 格号)")] public List<Test_RoomLayout.EdgeCellBlock> edgeBlocks = new List<Test_RoomLayout.EdgeCellBlock>();
    [Label("边缘格柱子(面 + 格号)")] public List<Test_RoomLayout.EdgeCellPillar> edgePillars = new List<Test_RoomLayout.EdgeCellPillar>();
    [Label("凹面/挖洞(本房间格坐标)")] public List<Test_RoomLayout.RoomRecess> recesses = new List<Test_RoomLayout.RoomRecess>();
}

/// <summary>
/// 房间拼装: 把"格坐标上的房间/通道"生成成一组 Test_RoomLayout 节点.
/// 数据由 房间拼装编辑器(窗口, Tools/房间拼装编辑器) 拖画维护, 也可以直接在这里改列表后点生成.
/// 相邻(边贴边)的格子会自动在该面开口当门洞, 开口两侧按开关补柱子.
/// </summary>
public class Test_DungeonLayout : MonoBehaviour
{
    [Label("预制体配置")] public RoomBlockSo prefabs;

    [Label("开口两侧的柱子")] public Test_RoomLayout.OpeningPillarKind openingPillars = Test_RoomLayout.OpeningPillarKind.OpeningPillar;
    [Label("栏杆缩放")] public float railingScale = 0.7f;
    [Min(1f)] [Label("柱子间隔(格)")] public float pillarInterval = 2f;
    [Label("楼梯每格抬升")] public float stairRise = 3f;
    [Label("生成墙体碰撞盒")] public bool addWallCollider = true;

    [Label("房间/通道(格坐标)")] public List<DungeonPiece> pieces = new List<DungeonPiece>();
    [ReadOnly] [Label("上次生成")] public string lastResult;

#if UNITY_EDITOR
    /// <summary>拼装网格以"半格"当最小单位: 0.5 格 = 1, 这样尺寸/位置都能落在半格上</summary>
    public static int ToHalf(float cells)
    {
        return Mathf.RoundToInt(cells * 2f);
    }

    /// <summary>半格单位 -> 格</summary>
    public static float FromHalf(int half)
    {
        return half * 0.5f;
    }

    /// <summary>把格数吸附到 0.5 的整数倍</summary>
    public static float SnapHalf(float cells)
    {
        return Mathf.Round(cells * 2f) * 0.5f;
    }

    /// <summary>某个整格(按格坐标)是不是被别的块占了(一格 = 两个半格)</summary>
    private static bool IsCellOccupied(Dictionary<Vector2Int, int> occupied, int cellCol, int cellRow, int selfIndex)
    {
        for (int hx = 0; hx < 2; hx++)
        {
            for (int hz = 0; hz < 2; hz++)
            {
                if (occupied.TryGetValue(new Vector2Int(cellCol * 2 + hx, cellRow * 2 + hz), out int owner) && owner != selfIndex)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>把每个块按半格登记成占用表(不改块本身的数据): 0.5 格错位也能对上; overlapHalfCells 是重复占用的半格数</summary>
    public static Dictionary<Vector2Int, int> BuildOccupied(List<DungeonPiece> pieces, out int overlapHalfCells)
    {
        Dictionary<Vector2Int, int> occupied = new Dictionary<Vector2Int, int>();
        overlapHalfCells = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            DungeonPiece piece = pieces[i];
            int hx0 = ToHalf(piece.col);
            int hz0 = ToHalf(piece.row);
            int halfCols = ToHalf(piece.cols);
            int halfRows = ToHalf(piece.rows);
            for (int hx = hx0; hx < hx0 + halfCols; hx++)
            {
                for (int hz = hz0; hz < hz0 + halfRows; hz++)
                {
                    Vector2Int half = new Vector2Int(hx, hz);
                    if (occupied.ContainsKey(half))
                    {
                        overlapHalfCells++;
                    }

                    occupied[half] = i;
                }
            }
        }

        return occupied;
    }

    [Button("打开编辑器")]
    public void OpenEditor()
    {
        Test_DungeonLayoutWindow.Open(this);
    }

    [Button("生成")]
    public void GenAll()
    {
        if (prefabs == null)
        {
            Debug.LogError("[DungeonLayout] 需要先指定 预制体配置(RoomBlockSo)");
            return;
        }

        if (prefabs.floorPrefab == null || prefabs.pillarPrefab == null)
        {
            Debug.LogError("[DungeonLayout] 预制体配置里缺 地板/柱子");
            return;
        }

        if (pieces.Count == 0)
        {
            Debug.LogError("[DungeonLayout] 还没有房间/通道");
            return;
        }

        // 单格尺寸与 Test_RoomLayout 用同一套算法, 保证格子和节点位置对得上
        Bounds floorBox = Test_RoomLayout.GetLayoutBox(prefabs.floorPrefab);
        float cellX = Mathf.Abs(floorBox.size.x);
        float cellZ = Mathf.Abs(floorBox.size.z);

        // 先把每个块的格坐标吸附到半格上(0.5 格也能画)
        for (int i = 0; i < pieces.Count; i++)
        {
            DungeonPiece piece = pieces[i];
            piece.col = SnapHalf(piece.col);
            piece.row = SnapHalf(piece.row);
            piece.cols = Mathf.Max(0.5f, SnapHalf(piece.cols));
            piece.rows = Mathf.Max(0.5f, SnapHalf(piece.rows));
            if (IsStair(piece) && (Mathf.Abs(piece.cols - Mathf.Round(piece.cols)) > 0.001f ||
                Mathf.Abs(piece.rows - Mathf.Round(piece.rows)) > 0.001f))
            {
                // 楼梯是逐格抬高的, 尺寸带半格就先按整格吸附(不然踏步/碰撞体和半格对不上)
                Debug.LogWarning($"[DungeonLayout] 楼梯/通道 ({piece.col},{piece.row}) 的尺寸带 0.5, 已按整格吸附");
                piece.cols = Mathf.Round(piece.cols);
                piece.rows = Mathf.Round(piece.rows);
            }
        }

        Dictionary<Vector2Int, int> occupied = BuildOccupied(pieces, out int overlapCount);

        transform.DestroyChildren();

        // 高度: 从第一个块(地面)出发, 碰到楼梯就按楼梯方向把后面的房间抬上去
        float[] elevations = CalcElevations(occupied);

        int roomCount = 0;
        int corridorCount = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            GenPiece(pieces[i], i, occupied, cellX, cellZ, elevations[i]);
            if (pieces[i].isCorridor)
            {
                corridorCount++;
            }
            else
            {
                roomCount++;
            }
        }

        lastResult = $"房间 {roomCount} 个, 通道 {corridorCount} 条, 占用 {occupied.Count * 0.25f:0.##} 格";
        if (overlapCount > 0)
        {
            Debug.LogWarning($"[DungeonLayout] 有 {overlapCount * 0.25f:0.##} 个格子被重复占用, 建议错开");
        }

        Debug.Log($"[DungeonLayout] 生成完成: {lastResult}");
    }

    [Button("清理生成物")]
    public void ClearAll()
    {
        transform.DestroyChildren();
        lastResult = "";
    }

    /// <summary>生成一个房间/通道: 一个 Test_RoomLayout 节点, 贴墙的面按相邻关系开口</summary>
    private void GenPiece(DungeonPiece piece, int index, Dictionary<Vector2Int, int> occupied, float cellX, float cellZ, float elevation)
    {
        string baseName = string.IsNullOrEmpty(piece.name) ? (piece.isCorridor ? "Corridor" : "Room") : piece.name;
        bool isStair = IsStair(piece);
        GameObject node = new GameObject($"{baseName}_{piece.col}_{piece.row}");
        node.transform.SetParent(transform, false);
        node.transform.localPosition = new Vector3(piece.col * cellX, elevation, -piece.row * cellZ);

        Test_RoomLayout room = node.AddComponent<Test_RoomLayout>();
        room.prefabs = prefabs;
        room.roomSize = new Vector2(piece.cols, piece.rows);
        room.genFloor = !isStair;      // 楼梯通道不铺地板, 由楼梯自己当路面
        room.genPillars = !isStair;    // 楼梯默认不摆柱子
        room.addWallCollider = addWallCollider && !isStair;   // 楼梯两侧改用平行四边形碰撞体(见 GenStairSideColliders)
        if (isStair)
        {
            room.stairUpFace = StairDirToFace(piece.stair);
            room.stairRise = stairRise;
        }

        room.wallFaces = piece.wallFaces;
        room.railingScale = railingScale;
        room.pillarInterval = pillarInterval;
        room.openings = CalcOpenings(piece, index, occupied, openingPillars);
        room.edgeBlocks = CalcEdgeBlocks(piece);
        room.edgePillars = CalcEdgePillars(piece);
        room.recesses = CalcRecesses(piece);
        room.GenRoom();

        if (isStair)
        {
            bool alongZ = piece.stair == StairDir.Up || piece.stair == StairDir.Down;
            if (alongZ ? piece.rows <= piece.cols : piece.cols <= piece.rows)
            {
                Debug.LogWarning($"[DungeonLayout] 楼梯方向和通道形状对不上: 通道 {piece.cols}列 x {piece.rows}行, " +
                    "上/下坡要竖着放(行数>列数), 左/右坡要横着放(列数>行数)");
            }

            GenStair(piece, node.transform, cellX, cellZ, index, occupied);
        }
    }

    /// <summary>是不是楼梯通道</summary>
    public static bool IsStair(DungeonPiece piece)
    {
        return piece.isCorridor && piece.stair != StairDir.None;
    }

    /// <summary>通道改成楼梯: 沿上坡方向逐格摆一个楼梯模块, 每格抬高 stairRise</summary>
    private void GenStair(DungeonPiece piece, Transform roomNode, float cellX, float cellZ, int selfIndex, Dictionary<Vector2Int, int> occupied)
    {
        if (prefabs.stairPrefab == null)
        {
            Debug.LogError("[DungeonLayout] 预制体配置里缺 楼梯");
            return;
        }

        bool alongZ = piece.stair == StairDir.Up || piece.stair == StairDir.Down;
        float cell = alongZ ? cellZ : cellX;
        int stepCount = Mathf.RoundToInt(alongZ ? piece.rows : piece.cols);   // 楼梯按整格算

        // 楼梯预制体默认由自己的 z=0 往 -z 爬, 所以节点放在"低端那一角", 用朝向决定爬向
        Vector3 lowCorner = Vector3.zero;
        float yaw = 0f;
        switch (piece.stair)
        {
            case StairDir.Down:                                             // 往 -z(画布下方)爬
                lowCorner = new Vector3(0f, 0f, 0f);
                yaw = 0f;
                break;
            case StairDir.Up:                                               // 往 +z(画布上方)爬
                // 这里 yaw=180 会把"宽度方向"镜像, 所以起点要放在 x=宽度 那一侧, 楼梯才正好落在通道格内
                lowCorner = new Vector3(piece.cols * cellX, 0f, -piece.rows * cellZ);
                yaw = 180f;
                break;
            case StairDir.Left:                                             // 往 -x(画布左)爬
                lowCorner = new Vector3(piece.cols * cellX, 0f, 0f);
                yaw = 90f;
                break;
            case StairDir.Right:                                            // 往 +x(画布右)爬
                lowCorner = new Vector3(0f, 0f, -cellZ);
                yaw = -90f;
                break;
        }

        GameObject stairNode = new GameObject("Stair");
        stairNode.transform.SetParent(roomNode, false);
        stairNode.transform.localPosition = lowCorner;
        stairNode.transform.localEulerAngles = new Vector3(0f, yaw, 0f);

        for (int i = 0; i < stepCount; i++)
        {
            GameObject step = BuildingTool.EditorInstancePrefab(prefabs.stairPrefab, stairNode.transform);
            step.name = $"{prefabs.stairPrefab.name}{i}";
            step.transform.localPosition = new Vector3(0f, i * stairRise, -i * cell);
        }

        GenStairSideColliders(piece, stairNode.transform, cellX, cellZ, occupied);
    }

    /// <summary>楼梯两侧碰撞体的厚度</summary>
    private const float StairSideColliderThickness = 0.6f;

    /// <summary>
    /// 楼梯两侧的碰撞体: 楼梯是斜面, 普通 BoxCollider 贴不住, 这里用一个"平行四边形"网格来做
    /// (底面顺坡度, 顶面与底面平行, 两端竖直), 每侧再按有没有通道口切段.
    /// </summary>
    private void GenStairSideColliders(DungeonPiece piece, Transform stairNode, float cellX, float cellZ, Dictionary<Vector2Int, int> occupied)
    {
        bool alongZ = piece.stair == StairDir.Up || piece.stair == StairDir.Down;
        float cell = alongZ ? cellZ : cellX;
        float pieceWidth = alongZ ? piece.cols * cellX : piece.rows * cellZ;
        float height = 8f * 0.7f;   // 和 BuildingTool 里给墙/栏杆补的最小高度保持一致
        int index = 0;

        for (int side = 0; side < 2; side++)
        {
            bool minSide = side == 0;
            Test_RoomLayout.RoomFace sideFace = alongZ
                ? (minSide ? Test_RoomLayout.RoomFace.XMin : Test_RoomLayout.RoomFace.XMax)
                : (minSide ? Test_RoomLayout.RoomFace.ZMin : Test_RoomLayout.RoomFace.ZMax);
            bool hasWall = (piece.wallFaces & sideFace) != Test_RoomLayout.RoomFace.None;

            foreach (Vector2Int span in CalcStairSideSpans(piece, occupied, minSide))
            {
                GameObject go = new GameObject(Test_RoomLayout.IndexedName("StairSideCollider", index++));
                go.transform.SetParent(stairNode, false);
                // 位置放在"这一段的低端"的踏步面上, 网格自己带坡度
                go.transform.localPosition = new Vector3(minSide ? 0f : pieceWidth, span.x * stairRise + prefabs.stairRailBase, -span.x * cell);
                go.layer = hasWall ? Layers.WALL_BLOCK : Layers.WALL;

                MeshCollider collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = GetStairSideMesh(span.y * cell, span.y * stairRise, height);
                collider.convex = true;
            }
        }
    }

    /// <summary>楼梯某一侧要放碰撞体的段(逐格看对面有没有别的块, 有就是通道口不放)</summary>
    private List<Vector2Int> CalcStairSideSpans(DungeonPiece piece, Dictionary<Vector2Int, int> occupied, bool minSide)
    {
        bool alongZ = piece.stair == StairDir.Up || piece.stair == StairDir.Down;
        int runCount = Mathf.RoundToInt(alongZ ? piece.rows : piece.cols);
        List<Vector2Int> spans = new List<Vector2Int>();
        int start = -1;

        for (int i = 0; i < runCount; i++)
        {
            bool blocked = HasNeighbourOnSide(piece, i, occupied, minSide);
            if (!blocked && start < 0)
            {
                start = i;
            }

            if (start >= 0 && (blocked || i == runCount - 1))
            {
                int end = blocked ? i - 1 : i;
                spans.Add(new Vector2Int(start, end - start + 1));
                start = -1;
            }
        }

        return spans;
    }

    /// <summary>楼梯第 i 格(从低端数)的这一侧外面是不是别人的格子</summary>
    /// <summary>楼梯第 i 格(从低端数)的这一侧外面是不是别人的格子(楼梯按整格算)</summary>
    private bool HasNeighbourOnSide(DungeonPiece piece, int runIndex, Dictionary<Vector2Int, int> occupied, bool minSide)
    {
        int baseCol = Mathf.RoundToInt(piece.col);
        int baseRow = Mathf.RoundToInt(piece.row);
        int stepCount = Mathf.RoundToInt(piece.stair == StairDir.Up || piece.stair == StairDir.Down ? piece.rows : piece.cols);
        int col;
        int row;
        switch (piece.stair)
        {
            case StairDir.Down:
                col = baseCol;
                row = baseRow + runIndex;
                break;
            case StairDir.Up:
                col = baseCol;
                row = baseRow + stepCount - 1 - runIndex;
                break;
            case StairDir.Right:
                col = baseCol + runIndex;
                row = baseRow;
                break;
            default:
                col = baseCol + stepCount - 1 - runIndex;
                row = baseRow;
                break;
        }

        bool alongZ = piece.stair == StairDir.Up || piece.stair == StairDir.Down;
        int sideCol = alongZ ? (minSide ? col - 1 : col + 1) : col;
        int sideRow = alongZ ? row : (minSide ? row - 1 : row + 1);
        return IsCellOccupied(occupied, sideCol, sideRow, -1);
    }

    /// <summary>取(没有就生成)平行四边形碰撞网格: 底面沿坡度, 顶面与之平行, 两端竖直</summary>
    private static Mesh GetStairSideMesh(float length, float rise, float height)
    {
        string name = $"StairSide_{length:0.##}x{rise:0.##}x{height:0.##}";
        string folder = "Assets/_Res/Item/Static/WallBlock/StairCollider";
        string path = $"{folder}/{name}.asset";

        Mesh mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null)
        {
            return mesh;
        }

        float halfThickness = StairSideColliderThickness * 0.5f;
        Vector3[] vertices = new Vector3[8];
        vertices[0] = new Vector3(-halfThickness, 0f, 0f);            // 底面: 低端
        vertices[1] = new Vector3(halfThickness, 0f, 0f);
        vertices[2] = new Vector3(halfThickness, rise, -length);      // 底面: 高端
        vertices[3] = new Vector3(-halfThickness, rise, -length);
        for (int i = 0; i < 4; i++)
        {
            vertices[i + 4] = vertices[i] + new Vector3(0f, height, 0f);
        }

        int[] triangles =
        {
            0, 2, 1, 0, 3, 2,     // 底
            4, 5, 6, 4, 6, 7,     // 顶
            0, 1, 5, 0, 5, 4,     // 低端面
            3, 7, 6, 3, 6, 2,     // 高端面
            0, 4, 7, 0, 7, 3,     // 一侧
            1, 2, 6, 1, 6, 5,     // 另一侧
        };

        mesh = new Mesh { name = name };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets/_Res/Item/Static/WallBlock", "StairCollider");
        }

        UnityEditor.AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    /// <summary>
    /// 算出这个房间/通道四个面上哪些格子要开口(对面那格属于别的房间/通道).
    /// 相邻关系按半格看, 开口的格号还是房间工具的口径: 每面从起点格数, 最后不足一整格的那半格也算一格.
    /// </summary>
    public static List<Test_RoomLayout.FaceOpening> CalcOpenings(DungeonPiece piece, int selfIndex,
        Dictionary<Vector2Int, int> occupied, Test_RoomLayout.OpeningPillarKind openingPillar)
    {
        List<Test_RoomLayout.FaceOpening> result = new List<Test_RoomLayout.FaceOpening>();
        int cellCols = Mathf.CeilToInt(piece.cols);
        int cellRows = Mathf.CeilToInt(piece.rows);
        int hx0 = ToHalf(piece.col);
        int hz0 = ToHalf(piece.row);
        int halfCols = ToHalf(piece.cols);
        int halfRows = ToHalf(piece.rows);

        // 沿z的两个面: 格号从 x 最小端开始; 每格看它盖住的那几个半格
        for (int c = 0; c < cellCols; c++)
        {
            for (int h = c * 2; h < Mathf.Min(c * 2 + 2, halfCols); h++)
            {
                AddOpening(result, Test_RoomLayout.RoomFace.ZMax, c, new Vector2Int(hx0 + h, hz0 - 1), selfIndex, occupied, openingPillar);
                AddOpening(result, Test_RoomLayout.RoomFace.ZMin, c, new Vector2Int(hx0 + h, hz0 + halfRows), selfIndex, occupied, openingPillar);
            }
        }

        // 沿x的两个面: 格号从 z 最小端开始, 也就是行号最大那一端
        for (int r = 0; r < cellRows; r++)
        {
            int faceIndex = cellRows - 1 - r;
            for (int h = r * 2; h < Mathf.Min(r * 2 + 2, halfRows); h++)
            {
                AddOpening(result, Test_RoomLayout.RoomFace.XMin, faceIndex, new Vector2Int(hx0 - 1, hz0 + h), selfIndex, occupied, openingPillar);
                AddOpening(result, Test_RoomLayout.RoomFace.XMax, faceIndex, new Vector2Int(hx0 + halfCols, hz0 + h), selfIndex, occupied, openingPillar);
            }
        }

        return result;
    }

    /// <summary>
    /// 把块上的"边缘格围墙"换成房间工具的格号: 左/右面的格号是"从面最上边那一格数起",
    /// 房间工具是从 z 最小端数, 所以要翻过来(跟开口的格号同一套算法).
    /// </summary>
    private static List<Test_RoomLayout.EdgeCellBlock> CalcEdgeBlocks(DungeonPiece piece)
    {
        List<Test_RoomLayout.EdgeCellBlock> result = new List<Test_RoomLayout.EdgeCellBlock>();
        int cellRows = Mathf.CeilToInt(piece.rows);
        foreach (Test_RoomLayout.EdgeCellBlock block in piece.edgeBlocks)
        {
            bool alongZFace = block.face == Test_RoomLayout.RoomFace.XMin || block.face == Test_RoomLayout.RoomFace.XMax;
            result.Add(new Test_RoomLayout.EdgeCellBlock
            {
                face = block.face,
                index = alongZFace ? cellRows - 1 - block.index : block.index,
                kind = block.kind,
            });
        }

        return result;
    }

    /// <summary>
    /// 把块上的"边缘格柱子"换成房间工具的格号: 左/右面的格号是"从面最上边那一格数起",
    /// 房间工具是从 z 最小端数, 所以要翻过来(跟开口的格号同一套算法).
    /// </summary>
    private static List<Test_RoomLayout.EdgeCellPillar> CalcEdgePillars(DungeonPiece piece)
    {
        List<Test_RoomLayout.EdgeCellPillar> result = new List<Test_RoomLayout.EdgeCellPillar>();
        int cellRows = Mathf.CeilToInt(piece.rows);
        foreach (Test_RoomLayout.EdgeCellPillar edge in piece.edgePillars)
        {
            bool alongZFace = edge.face == Test_RoomLayout.RoomFace.XMin || edge.face == Test_RoomLayout.RoomFace.XMax;
            result.Add(new Test_RoomLayout.EdgeCellPillar
            {
                face = edge.face,
                index = alongZFace ? cellRows - 1 - edge.index : edge.index,
                kind = edge.kind,
            });
        }

        return result;
    }

    /// <summary>
    /// 把块上的"凹面/挖洞"换成房间工具的格号: 块的行从节点原点那一侧往下数,
    /// 房间工具是从 z 最小端数, 所以行号要翻过来(跟上面开口的格号同一套算法).
    /// </summary>
    private static List<Test_RoomLayout.RoomRecess> CalcRecesses(DungeonPiece piece)
    {
        List<Test_RoomLayout.RoomRecess> result = new List<Test_RoomLayout.RoomRecess>();
        int cellCols = Mathf.CeilToInt(piece.cols);
        int cellRows = Mathf.CeilToInt(piece.rows);
        foreach (Test_RoomLayout.RoomRecess recess in piece.recesses)
        {
            int rows = Mathf.Max(1, recess.rows);
            result.Add(new Test_RoomLayout.RoomRecess
            {
                col = Mathf.Clamp(recess.col, 0, Mathf.Max(0, cellCols - 1)),
                row = Mathf.Clamp(cellRows - recess.row - rows, 0, Mathf.Max(0, cellRows - rows)),
                cols = Mathf.Min(Mathf.Max(1, recess.cols), cellCols),
                rows = Mathf.Min(rows, cellRows),
            });
        }

        return result;
    }

    private static void AddOpening(List<Test_RoomLayout.FaceOpening> result, Test_RoomLayout.RoomFace face, int faceIndex,
        Vector2Int neighbourHalf, int selfIndex, Dictionary<Vector2Int, int> occupied, Test_RoomLayout.OpeningPillarKind openingPillar)
    {
        if (!occupied.TryGetValue(neighbourHalf, out int owner) || owner == selfIndex)
        {
            return;
        }

        // 半格是逐格看的, 同一格可能被看好几次
        if (result.Exists(x => x.face == face && x.index == faceIndex))
        {
            return;
        }

        result.Add(new Test_RoomLayout.FaceOpening { face = face, index = faceIndex, pillar = openingPillar });
    }

    /// <summary>一个邻居块 + 从当前块走过去的高度差</summary>
    private struct Neighbour
    {
        public int pieceIndex;
        public float rise;
    }

    /// <summary>算每个块的高度: 从第一个块(地面)出发, 穿过楼梯时按上坡方向抬升</summary>
    private float[] CalcElevations(Dictionary<Vector2Int, int> occupied)
    {
        float[] levels = new float[pieces.Count];
        bool[] visited = new bool[pieces.Count];

        Queue<int> queue = new Queue<int>();
        visited[0] = true;
        queue.Enqueue(0);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (Neighbour neighbour in FindNeighbours(pieces[current], current, occupied))
            {
                float level = levels[current] + neighbour.rise;
                if (visited[neighbour.pieceIndex])
                {
                    if (Mathf.Abs(levels[neighbour.pieceIndex] - level) > 0.01f)
                    {
                        Debug.LogWarning($"[DungeonLayout] 高度对不上: 块 {neighbour.pieceIndex}({pieces[neighbour.pieceIndex].col},{pieces[neighbour.pieceIndex].row}) 从两边推出来是 {levels[neighbour.pieceIndex]} 和 {level}, 已按先算到的为准");
                    }

                    continue;
                }

                visited[neighbour.pieceIndex] = true;
                levels[neighbour.pieceIndex] = level;
                queue.Enqueue(neighbour.pieceIndex);
            }
        }

        // 整体平移到最低那块为 0, 让最下面的房间落在地面高度
        float lowest = 0f;
        foreach (float level in levels)
        {
            lowest = Mathf.Min(lowest, level);
        }

        for (int i = 0; i < levels.Length; i++)
        {
            levels[i] -= lowest;
        }

        return levels;
    }

    /// <summary>找和这个块贴边的邻居块(去重), 带上走过去的高度差</summary>
    private List<Neighbour> FindNeighbours(DungeonPiece piece, int index, Dictionary<Vector2Int, int> occupied)
    {
        List<Neighbour> result = new List<Neighbour>();
        List<int> found = new List<int>();
        int hx0 = ToHalf(piece.col);
        int hz0 = ToHalf(piece.row);
        int halfCols = ToHalf(piece.cols);
        int halfRows = ToHalf(piece.rows);

        // 相邻关系都按半格看(0.5 格错位也能连上); 同一个邻居块只记一次
        for (int h = 0; h < halfCols; h++)
        {
            AddNeighbour(result, found, piece, index, occupied, Test_RoomLayout.RoomFace.ZMax, new Vector2Int(hx0 + h, hz0 - 1));
            AddNeighbour(result, found, piece, index, occupied, Test_RoomLayout.RoomFace.ZMin, new Vector2Int(hx0 + h, hz0 + halfRows));
        }

        for (int h = 0; h < halfRows; h++)
        {
            AddNeighbour(result, found, piece, index, occupied, Test_RoomLayout.RoomFace.XMin, new Vector2Int(hx0 - 1, hz0 + h));
            AddNeighbour(result, found, piece, index, occupied, Test_RoomLayout.RoomFace.XMax, new Vector2Int(hx0 + halfCols, hz0 + h));
        }

        return result;
    }

    private void AddNeighbour(List<Neighbour> result, List<int> found, DungeonPiece piece, int index,
        Dictionary<Vector2Int, int> occupied, Test_RoomLayout.RoomFace myFace, Vector2Int neighbourCell)
    {
        if (!occupied.TryGetValue(neighbourCell, out int other) || other == index || found.Contains(other))
        {
            return;
        }

        found.Add(other);
        DungeonPiece otherPiece = pieces[other];
        float rise = 0f;

        // 我是楼梯, 从我上坡那一面出去 -> 邻居更高
        if (IsStair(piece) && myFace == StairDirToFace(piece.stair))
        {
            rise += StairHeight(piece);
        }

        // 邻居是楼梯, 它的上坡面正对着我 -> 我在它上面 -> 从我这走过去要下来
        if (IsStair(otherPiece) && OppositeFace(myFace) == StairDirToFace(otherPiece.stair))
        {
            rise -= StairHeight(otherPiece);
        }

        result.Add(new Neighbour { pieceIndex = other, rise = rise });
    }

    /// <summary>楼梯上坡方向对应哪个面</summary>
    private static Test_RoomLayout.RoomFace StairDirToFace(StairDir dir)
    {
        switch (dir)
        {
            case StairDir.Up: return Test_RoomLayout.RoomFace.ZMax;
            case StairDir.Down: return Test_RoomLayout.RoomFace.ZMin;
            case StairDir.Left: return Test_RoomLayout.RoomFace.XMin;
            case StairDir.Right: return Test_RoomLayout.RoomFace.XMax;
        }

        return Test_RoomLayout.RoomFace.None;
    }

    /// <summary>对面那个面</summary>
    private static Test_RoomLayout.RoomFace OppositeFace(Test_RoomLayout.RoomFace face)
    {
        switch (face)
        {
            case Test_RoomLayout.RoomFace.ZMax: return Test_RoomLayout.RoomFace.ZMin;
            case Test_RoomLayout.RoomFace.ZMin: return Test_RoomLayout.RoomFace.ZMax;
            case Test_RoomLayout.RoomFace.XMin: return Test_RoomLayout.RoomFace.XMax;
            case Test_RoomLayout.RoomFace.XMax: return Test_RoomLayout.RoomFace.XMin;
        }

        return Test_RoomLayout.RoomFace.None;
    }

    /// <summary>楼梯总抬升 = 每格抬升 × 格数</summary>
    private float StairHeight(DungeonPiece piece)
    {
        bool alongZ = piece.stair == StairDir.Up || piece.stair == StairDir.Down;
        int stepCount = Mathf.RoundToInt(alongZ ? piece.rows : piece.cols);   // 楼梯按整格算
        return stepCount * stairRise;
    }
#endif
}
