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

    [InfoBox("层级: -1 = 按原来的配置(顺着楼梯自动推算高度); 0 = 地面; 1 = 强制升高一层, 一层的高度 = 1 格楼梯的抬升(跟着上面的「楼梯每格抬升」走)。只动这一间, 不带动旁边的房间/通道")]
    [Label("层级(-1=自动)")] public int level = -1;

    [Label("哪几面有墙")] public Test_RoomLayout.RoomFace wallFaces = Test_RoomLayout.RoomFace.AllFaces;
    [Label("支撑柱(四个角)")] public Test_RoomLayout.SupportCorner supportCorners = Test_RoomLayout.SupportCorner.None;
    [Label("支撑柱宽度(0=等比)")] public float supportWidth;

    [InfoBox("支撑柱向外偏移: 0 = 柱子外侧面贴住房间边界; 值为柱子凸出房间边界的长度, 等于柱宽的一半时柱子中心正好压在房间角上")]
    [Label("支撑柱向外偏移")] public float supportOffset;
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

    [InfoBox("开口(门洞)两侧补的柱子类型。贴墙的面不受这里影响: 那一面是墙时, 门口两侧一律用普通柱子")]
    [Label("开口两侧的柱子")] public Test_RoomLayout.OpeningPillarKind openingPillars = Test_RoomLayout.OpeningPillarKind.OpeningPillar;
    [Label("栏杆缩放")] public float railingScale = 0.7f;
    [Min(1f)] [Label("柱子间隔(格)")] public float pillarInterval = 2f;
    [Label("楼梯每格抬升")] public float stairRise = 3f;
    [Label("生成墙体碰撞盒")] public bool addWallCollider = true;
    [Label("生成通道连线点(PassLine)")] public bool genPassPoints = true;

    [Label("通道点外扩距离")]
    [Tooltip("通道端点从端面中心往通道外侧推出去的距离(局部单位)。端点正好压在墙角/楼梯侧板上时敌人容易卡住, " +
             "往相邻块内部挪一点, 让路径点离开墙边")]
    public float passPointOutset = 1f;

    [Label("房间/通道(格坐标)")] public List<DungeonPiece> pieces = new List<DungeonPiece>();
    [ReadOnly] [Label("上次生成")] public string lastResult;

#if UNITY_EDITOR
    /// <summary>
    /// 一层的高度 = 1 格楼梯的抬升, 直接跟着「楼梯每格抬升」走(楼梯预制体本身也是这么高), 不用单独填参数。
    /// </summary>
    public float FloorHeight => stairRise;

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
        // 房间节点留下来给通道点分组用(端面贴着哪个房间就归到哪一组)
        Dictionary<int, Transform> pieceNodes = new Dictionary<int, Transform>();
        for (int i = 0; i < pieces.Count; i++)
        {
            pieceNodes[i] = GenPiece(pieces[i], i, occupied, cellX, cellZ, elevations[i]).transform;
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

        // 通道连线点放在房间都生成完再做: 只依赖格坐标和高度, 不依赖房间节点
        if (genPassPoints)
        {
            int passLineCount = GenPassPoints(occupied, cellX, cellZ, elevations, pieceNodes);
            if (passLineCount > 0)
            {
                lastResult += $", 通道连线 {passLineCount} 条";
            }
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
    private GameObject GenPiece(DungeonPiece piece, int index, Dictionary<Vector2Int, int> occupied, float cellX, float cellZ, float elevation)
    {
        string baseName = PieceBaseName(piece);
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

        // 支撑柱放在最后: 高度要量"已经生成完"的整个房间底面(含楼梯)
        GenSupportPillars(piece, node.transform, cellX, cellZ);

        return node;
    }

    /// <summary>房间/通道节点的名字前缀: 没填名字就按类型兜底</summary>
    private static string PieceBaseName(DungeonPiece piece)
    {
        return string.IsNullOrEmpty(piece.name) ? (piece.isCorridor ? "Corridor" : "Room") : piece.name;
    }

    /// <summary>通道连线点统一挂在这个子节点下, 由它身上的 PassPointCollector 收集</summary>
    private const string PassPointNodeName = "PassPoints";

    /// <summary>
    /// 通道连线: 每条通道在两端面中心各放一个空节点, 两个节点组成一条 PassLine,
    /// 全部塞进同一个 PassPointCollector(挂在 PassPoints 节点上)。返回生成了几条 PassLine。
    /// <para>同时按"端面贴着哪个房间"登记房间分组: 同一房间里的门洞之间没有墙, 运行时直接当作互通,
    /// 不需要再做射线检测(见 <see cref="PassPathFinder"/>)。</para>
    /// </summary>
    private int GenPassPoints(Dictionary<Vector2Int, int> occupied, float cellX, float cellZ, float[] elevations,
        Dictionary<int, Transform> pieceNodes)
    {
        List<int> corridorIndexes = new List<int>();
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].isCorridor)
            {
                corridorIndexes.Add(i);
            }
        }

        if (corridorIndexes.Count == 0)
        {
            return 0;
        }

        GameObject root = new GameObject(PassPointNodeName);
        root.transform.SetParent(transform, false);
        PassPointCollector collector = root.AddComponent<PassPointCollector>();
        collector.lines = new List<PassLine>();
        collector.rooms = new List<PassRoom>();

        // 每个房间先开一个空分组, 通道端点生成时按端面贴着的房间往里塞
        Dictionary<int, PassRoom> roomOf = new Dictionary<int, PassRoom>();
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].isCorridor)
            {
                continue;
            }

            Transform roomNode;
            pieceNodes.TryGetValue(i, out roomNode);
            PassRoom room = new PassRoom
            {
                roomNode = roomNode,
                points = new List<Transform>(),
            };
            collector.rooms.Add(room);
            roomOf[i] = room;
        }

        foreach (int index in corridorIndexes)
        {
            DungeonPiece piece = pieces[index];
            collector.lines.Add(GenPassLine(piece, index, occupied, root.transform, cellX, cellZ, elevations[index], roomOf));
        }

        // 一个端点都没接到的房间(没有通道贴着它)不用留在列表里
        collector.rooms.RemoveAll(room => room == null || room.points.Count == 0);

        return collector.lines.Count;
    }

    /// <summary>
    /// 给一条通道生成一条 PassLine: 两端面中心各一个空节点。
    /// 楼梯通道从低端指向上坡那一端, 上坡端的点按楼梯总抬升抬高(两端高度不同)。
    /// </summary>
    private PassLine GenPassLine(DungeonPiece piece, int index, Dictionary<Vector2Int, int> occupied,
        Transform parent, float cellX, float cellZ, float elevation, Dictionary<int, PassRoom> roomOf)
    {
        GetPassEndFaces(piece, index, occupied, out Test_RoomLayout.RoomFace startFace, out Test_RoomLayout.RoomFace endFace);

        // 楼梯的上坡端是高的那一头: 只有那个点要抬起来
        Test_RoomLayout.RoomFace upFace = IsStair(piece) ? StairDirToFace(piece.stair) : Test_RoomLayout.RoomFace.None;
        float startY = startFace == upFace ? elevation + StairHeight(piece) : elevation;
        float endY = endFace == upFace ? elevation + StairHeight(piece) : elevation;

        string baseName = PieceBaseName(piece);
        PassLine line = new PassLine { points = new List<Transform>() };
        Transform startPoint = CreatePassPoint(parent, $"{baseName}_Pass0",
            FaceCenterLocal(piece, startFace, cellX, cellZ, startY, passPointOutset));
        Transform endPoint = CreatePassPoint(parent, $"{baseName}_Pass1",
            FaceCenterLocal(piece, endFace, cellX, cellZ, endY, passPointOutset));
        line.points.Add(startPoint);
        line.points.Add(endPoint);

        // 端面外面贴着的如果是房间, 这个端点就归到那个房间的分组(房间内必定互通)
        RegisterRoomPoint(occupied, piece, startFace, startPoint, roomOf);
        RegisterRoomPoint(occupied, piece, endFace, endPoint, roomOf);

        return line;
    }

    /// <summary>通道端面外面贴着的如果是房间, 把这个端点登记进那个房间的分组</summary>
    private static void RegisterRoomPoint(Dictionary<Vector2Int, int> occupied, DungeonPiece piece,
        Test_RoomLayout.RoomFace face, Transform point, Dictionary<int, PassRoom> roomOf)
    {
        if (face == Test_RoomLayout.RoomFace.None)
        {
            return;
        }

        int neighbor = GetNeighborPiece(occupied, piece, face);
        PassRoom room;
        if (neighbor >= 0 && roomOf.TryGetValue(neighbor, out room))
        {
            room.points.Add(point);
        }
    }

    /// <summary>某个面正外侧那一格属于哪个块(没有返回 -1). 占用表是半格精度, 这里对应挪一格半格单位</summary>
    private static int GetNeighborPiece(Dictionary<Vector2Int, int> occupied, DungeonPiece piece, Test_RoomLayout.RoomFace face)
    {
        int hx0 = ToHalf(piece.col);
        int hz0 = ToHalf(piece.row);
        int halfCols = ToHalf(piece.cols);
        int halfRows = ToHalf(piece.rows);
        int hx;
        int hz;
        switch (face)
        {
            case Test_RoomLayout.RoomFace.XMin:
                hx = hx0 - 1;
                hz = hz0 + halfRows / 2;
                break;
            case Test_RoomLayout.RoomFace.XMax:
                hx = hx0 + halfCols;
                hz = hz0 + halfRows / 2;
                break;
            // row 增大 = z 减小: ZMax 在 row 小的那一侧, ZMin 在 row 大的那一侧
            case Test_RoomLayout.RoomFace.ZMax:
                hx = hx0 + halfCols / 2;
                hz = hz0 - 1;
                break;
            case Test_RoomLayout.RoomFace.ZMin:
                hx = hx0 + halfCols / 2;
                hz = hz0 + halfRows;
                break;
            default:
                return -1;
        }

        int owner;
        return occupied.TryGetValue(new Vector2Int(hx, hz), out owner) ? owner : -1;
    }

    /// <summary>空节点: 只在层级里占个位置, 被 PassLine 收集后给后续逻辑取坐标用</summary>
    private static Transform CreatePassPoint(Transform parent, string name, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    /// <summary>
    /// 通道两端是哪两个面: 楼梯按上坡方向那条轴; 普通通道优先用"对面也有开口"的那条轴,
    /// 判断不出来(四面都通或四面都不通)就按较长的那条边。
    /// startFace 是连线起点: 楼梯是低端, 普通通道是坐标较小的一端。
    /// </summary>
    private static void GetPassEndFaces(DungeonPiece piece, int index, Dictionary<Vector2Int, int> occupied,
        out Test_RoomLayout.RoomFace startFace, out Test_RoomLayout.RoomFace endFace)
    {
        if (IsStair(piece))
        {
            endFace = StairDirToFace(piece.stair);      // 楼梯: 从低端走到上坡面
            startFace = OppositeFace(endFace);
            return;
        }

        List<Test_RoomLayout.FaceOpening> openings =
            CalcOpenings(piece, index, occupied, Test_RoomLayout.OpeningPillarKind.OpeningPillar);
        bool openX = openings.Exists(x => x.face == Test_RoomLayout.RoomFace.XMin) &&
                     openings.Exists(x => x.face == Test_RoomLayout.RoomFace.XMax);
        bool openZ = openings.Exists(x => x.face == Test_RoomLayout.RoomFace.ZMin) &&
                     openings.Exists(x => x.face == Test_RoomLayout.RoomFace.ZMax);

        bool alongX;
        if (openX != openZ)
        {
            alongX = openX;                             // 只有一个方向的对面都通, 那就是通道的走向
        }
        else
        {
            alongX = piece.cols >= piece.rows;          // 判断不出来就按长边
        }

        startFace = alongX ? Test_RoomLayout.RoomFace.XMin : Test_RoomLayout.RoomFace.ZMin;
        endFace = alongX ? Test_RoomLayout.RoomFace.XMax : Test_RoomLayout.RoomFace.ZMax;
    }

    /// <summary>
    /// 通道某个面的端点在拼装根节点下的局部坐标: 另一条轴取中点, 再沿该面的外法线往相邻块里推 <paramref name="outset"/>。
    /// <para>不推的话端点正好压在块边界上, 也就是墙角/楼梯侧板的位置, 敌人走到那里会顶住碰撞体卡着不动。</para>
    /// </summary>
    private static Vector3 FaceCenterLocal(DungeonPiece piece, Test_RoomLayout.RoomFace face, float cellX, float cellZ,
        float y, float outset)
    {
        float xMin = piece.col * cellX;
        float xMax = xMin + piece.cols * cellX;
        float zMax = -piece.row * cellZ;                 // 画布上方
        float zMin = zMax - piece.rows * cellZ;          // 画布下方
        switch (face)
        {
            case Test_RoomLayout.RoomFace.XMin:
                return new Vector3(xMin - outset, y, (zMin + zMax) * 0.5f);
            case Test_RoomLayout.RoomFace.XMax:
                return new Vector3(xMax + outset, y, (zMin + zMax) * 0.5f);
            case Test_RoomLayout.RoomFace.ZMax:
                return new Vector3((xMin + xMax) * 0.5f, y, zMax + outset);
            default:
                return new Vector3((xMin + xMax) * 0.5f, y, zMin - outset);
        }
    }

    /// <summary>支撑柱的最小高度: 低于这个值说明房间已经贴地, 不用撑</summary>
    private const float MinSupportHeight = 0.15f;

    /// <summary>支撑柱挂在这个子节点下, 量"建筑底面"时要跳过它</summary>
    private const string SupportNodeName = "Support";

    /// <summary>四个角的标记, 与 <see cref="Test_RoomLayout.SupportCorner"/> 一一对应</summary>
    private static readonly Test_RoomLayout.SupportCorner[] SupportCornerFlags =
    {
        Test_RoomLayout.SupportCorner.XMinZMin,
        Test_RoomLayout.SupportCorner.XMaxZMin,
        Test_RoomLayout.SupportCorner.XMinZMax,
        Test_RoomLayout.SupportCorner.XMaxZMax,
    };

    /// <summary>每个角在 X 方向贴的是哪条边: -1 = XMin 边(柱子往 +X 长), +1 = XMax 边(往 -X 长)</summary>
    private static readonly int[] SupportCornerXSign = { -1, 1, -1, 1 };

    /// <summary>每个角在 Z 方向贴的是哪条边: -1 = ZMin 边(柱子往 +Z 长), +1 = ZMax 边(往 -Z 长)</summary>
    private static readonly int[] SupportCornerZSign = { -1, -1, 1, 1 };

    /// <summary>
    /// 支撑柱: 配了哪几个角就在哪几个角摆一根, 从地面(拼装根节点的高度)竖直顶到建筑底面,
    /// 所以抬高的房间/通道下面会自动补上整根柱子, 贴地的房间会被跳过(高度约为 0).
    /// </summary>
    private void GenSupportPillars(DungeonPiece piece, Transform roomNode, float cellX, float cellZ)
    {
        if (piece.supportCorners == Test_RoomLayout.SupportCorner.None)
        {
            return;
        }

        GameObject prefab = prefabs.supportPillarPrefab != null ? prefabs.supportPillarPrefab : prefabs.pillarPrefab;
        if (prefab == null)
        {
            Debug.LogError("[DungeonLayout] 有房间配了支撑柱, 但预制体配置里既没有 支撑柱 也没有 柱子");
            return;
        }

        // 地面 = 拼装根节点的高度: 高度计算已经把最低的那块平移到 0 (见 CalcElevations)
        float groundY = transform.position.y;
        // 建筑底面 = 房间节点下所有渲染物的最低点
        float bottomY = GetBottomY(roomNode);
        float height = bottomY - groundY;
        if (height <= MinSupportHeight)
        {
            return;
        }

        Bounds box = Test_RoomLayout.GetLayoutBox(prefab);
        if (box.size.y < 0.0001f)
        {
            Debug.LogError($"[DungeonLayout] 支撑柱 {prefab.name} 量不出高度");
            return;
        }

        // 按高度拉伸: 拉伸后网格底边对齐地面, 顶边正好顶住建筑底面
        float scaleY = height / box.size.y;
        float meshBottom = box.center.y - box.size.y * 0.5f;   // 网格底边相对预制体原点
        float y = groundY - meshBottom * scaleY;

        // 横截面: 0 表示等比放大(和高度用同一个缩放), 否则直接用配置值当 localScale 的 xz
        float scaleXZ = piece.supportWidth > 0f ? piece.supportWidth : scaleY;

        float xMin = roomNode.position.x;
        float xMax = xMin + piece.cols * cellX;
        float zMax = roomNode.position.z;                        // 画布上方
        float zMin = zMax - piece.rows * cellZ;                  // 画布下方

        // 横截面缩放后的实际半宽, 以及网格中心相对预制体原点的偏移(原点不一定在网格中心)
        float halfX = box.size.x * scaleXZ * 0.5f;
        float halfZ = box.size.z * scaleXZ * 0.5f;
        float centerOffsetX = box.center.x * scaleXZ;
        float centerOffsetZ = box.center.z * scaleXZ;

        // 四个角在 XZ 平面上的世界位置: 和 SupportCornerFlags / SupportCornerXSign 一一对应
        Vector2[] corners =
        {
            new Vector2(xMin, zMin),      // 左下
            new Vector2(xMax, zMin),      // 右下
            new Vector2(xMin, zMax),      // 左上
            new Vector2(xMax, zMax),      // 右上
        };

        // 支撑柱统一挂在一个子节点下: 层级清楚, 量"建筑底面"时也能整棵跳过
        Transform supportNode = roomNode.Find(SupportNodeName);
        if (supportNode == null)
        {
            GameObject supportGo = new GameObject(SupportNodeName);
            supportGo.transform.SetParent(roomNode, false);
            supportNode = supportGo.transform;
        }
        else
        {
            // 重复生成时先清掉旧的
            supportNode.DestroyChildren();
        }

        // 向外偏移: 0 = 柱子的外侧面贴住房间边界(整根柱子都在房间里);
        // 值 = 柱子凸出房间边界的长度, 等于半宽时柱子中心正好压在房间角上
        float outward = Mathf.Max(0f, piece.supportOffset);
        float insetX = halfX - outward;
        float insetZ = halfZ - outward;

        for (int i = 0; i < corners.Length; i++)
        {
            if ((piece.supportCorners & SupportCornerFlags[i]) == 0)
            {
                continue;
            }

            GameObject go = BuildingTool.EditorInstancePrefab(prefab, supportNode);
            go.name = Test_RoomLayout.IndexedName("SupportPillar", i);
            go.transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);

            // 角对角: 默认外侧面贴住房间边界(不是中心点压在角上), 整根柱子都落在房间范围内; 配了向外偏移就往外推
            float cornerX = corners[i].x;
            float cornerZ = corners[i].y;        // Vector2 的 y 存的是 Z
            float centerX = SupportCornerXSign[i] < 0 ? cornerX + insetX : cornerX - insetX;
            float centerZ = SupportCornerZSign[i] < 0 ? cornerZ + insetZ : cornerZ - insetZ;
            go.transform.position = new Vector3(centerX - centerOffsetX, y, centerZ - centerOffsetZ);
        }
    }

    /// <summary>
    /// 节点下所有渲染物的最低点(世界 y), 一个都没有时返回 0.
    /// 跳过名为 Support 的子树: 支撑柱自己的底边就在地面上, 算进去会把高度量成 0.
    /// </summary>
    private static float GetBottomY(Transform node)
    {
        bool hasBounds = false;
        float bottom = 0f;
        foreach (Renderer renderer in node.GetComponentsInChildren<Renderer>(true))
        {
            if (IsUnderSupport(renderer.transform, node))
            {
                continue;
            }

            bottom = hasBounds ? Mathf.Min(bottom, renderer.bounds.min.y) : renderer.bounds.min.y;
            hasBounds = true;
        }

        return bottom;
    }

    /// <summary>这个渲染物是不是挂在支撑柱子树里</summary>
    private static bool IsUnderSupport(Transform tf, Transform roomNode)
    {
        while (tf != null && tf != roomNode)
        {
            if (tf.name == SupportNodeName)
            {
                return true;
            }

            tf = tf.parent;
        }

        return false;
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

    /// <summary>
    /// 算每个块的高度: 从第一个块(地面)出发, 穿过楼梯时按上坡方向抬升。
    /// 配了「层级」的块最后再单独覆盖掉, 直接摆到 层级 × 每层高度 上(只动它自己, 不带动旁边的块)。
    /// </summary>
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

        // 配了层级的块: 强制摆到 层级 × 一层的高度, 一层 = 1 格楼梯的抬升(见 FloorHeight), 不再看楼梯推出来多少
        for (int i = 0; i < levels.Length; i++)
        {
            if (pieces[i].level >= 0)
            {
                levels[i] = pieces[i].level * FloorHeight;
            }
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
