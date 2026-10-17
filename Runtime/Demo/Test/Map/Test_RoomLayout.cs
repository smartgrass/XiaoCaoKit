using System.Collections.Generic;
using GG.Extensions;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;
using XiaoCao.Map;

/// <summary>
/// 房间排版工具: 输入房间尺寸(列x 行y), 一键铺地板 / 围栏杆 / 沿边按间距摆柱子, 贴墙的面用墙面代替栏杆.
/// 还能在房间里挖"凹面/挖洞": 划一块区域把它下面的地板挖掉, 贴着房间边的按凹面处理(那一面的栏杆/墙跟着凹进去, 把区域边缘围起来), 不贴边的就只是地上的洞.
/// 边缘还能逐格点名: 「边缘格围墙」把某一格的墙/栏杆换成 墙/栏杆/空, 「边缘格柱子」把某一格的柱子换成 柱子/开口柱子/空.
/// 生成结构与 Assets/_Res/Item/Static/room/FloorRect.prefab 类似:
/// 根节点下是 1个地板节点(Test_MapLayout) + 若干边栏杆节点(Test_WallLayout) + 若干边墙面节点(Test_MapLayout) + 4个边柱子节点(Test_MapLayout),
/// 各部分都留着对应工具的参数, 可以单独在 Inspector 上调整后再点它自己的生成按钮.
/// </summary>
public class Test_RoomLayout : MonoBehaviour
{
    [Label("预制体配置")] public RoomBlockSo prefabs;

    [InfoBox("尺寸: x = 列数(x方向), y = 行数(z方向). 例: (4,3) 表示 4列3行; 可以带 0.5 格, 差的那截用小地块补")]
    [Label("房间尺寸(列x, 行y)")] public Vector2 roomSize = new Vector2(4, 3);

    [Label("地板追加间距")] public Vector3 addValue;
    [Label("生成地板")] public bool genFloor = true;
    [Label("生成柱子")] public bool genPillars = true;

    [InfoBox("楼梯段: 上坡面不为None时按楼梯处理 —— 两侧的墙/栏杆跟着踏步逐格抬高, 不铺地板")]
    [EnumFlags] [Label("楼梯上坡面")] public RoomFace stairUpFace = RoomFace.None;
    [Label("楼梯每格抬升")] public float stairRise = 3f;

    [Label("栏杆缩放")] public float railingScale = 0.7f;
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
        [InspectorName("无")] None = 0,
        [InspectorName("左")] XMin = 1,
        [InspectorName("右")] XMax = 2,
        [InspectorName("下")] ZMin = 4,
        [InspectorName("上")] ZMax = 8,
        [InspectorName("四面")] AllFaces = XMin | XMax | ZMin | ZMax,
    }

    /// <summary>面上的开口(门洞/通道口): index 是这一面上第几格, 从面的起点格算起</summary>
    [System.Serializable]
    public class FaceOpening
    {
        public RoomFace face;
        [Min(0)] public int index;
        [Label("开口两侧的柱子")] public OpeningPillarKind pillar = OpeningPillarKind.OpeningPillar;
    }

    [InfoBox("开口格不放墙也不放栏杆, 用来接通道; 每项 = 哪个面 + 面内第几格")]
    [Label("开口(面 + 格号)")] public List<FaceOpening> openings = new List<FaceOpening>();

    /// <summary>开口(门洞)两侧补什么柱子: 开口柱子 / 柱子 / 空.
    /// 值按 0 = 空, 1 = 开口柱子 排: 老数据是 bool, false/true 正好落到"空/开口柱子"上.</summary>
    public enum OpeningPillarKind
    {
        [InspectorName("空")] Empty = 0,
        [InspectorName("开口柱子")] OpeningPillar = 1,
        [InspectorName("柱子")] Pillar = 2,
    }

    /// <summary>边缘格上放什么: 墙体 / 栏杆 / 空(什么都不放)</summary>
    public enum EdgeBlockKind
    {
        [InspectorName("墙体")] Wall = 0,
        [InspectorName("栏杆")] Railing = 1,
        [InspectorName("空")] Empty = 2,
    }

    /// <summary>单独指定某一个边缘格放什么: 面 + 面内格号, 覆盖"贴墙的面=墙, 其它=栏杆"的默认</summary>
    [System.Serializable]
    public class EdgeCellBlock
    {
        public RoomFace face;
        [Min(0)] public int index;
        [Label("类型")] public EdgeBlockKind kind = EdgeBlockKind.Wall;
    }

    [InfoBox("按 面 + 格号 挑出某一个边缘格, 把这一格原本的围墙/栏杆换成类型里的(墙体/栏杆/空); 空 = 这一格既不放墙也不放栏杆")]
    [Label("边缘格围墙(面 + 格号)")] public List<EdgeCellBlock> edgeBlocks = new List<EdgeCellBlock>();

    /// <summary>边缘格上摆什么柱子: 普通柱子 / 开口柱子 / 空(不摆)</summary>
    public enum EdgePillarKind
    {
        [InspectorName("柱子")] Pillar = 0,
        [InspectorName("开口柱子")] OpeningPillar = 1,
        [InspectorName("空")] Empty = 2,
    }

    /// <summary>单独指定某一个边缘格的柱子: 面 + 面内格号</summary>
    [System.Serializable]
    public class EdgeCellPillar
    {
        public RoomFace face;
        [Min(0)] public int index;
        [Label("类型")] public EdgePillarKind kind = EdgePillarKind.Pillar;
    }

    [InfoBox("按 面 + 格号 挑出某一个边缘格: 先把这一格原有的柱子清掉, 再按类型补一根(柱子/开口柱子/空 = 不摆)")]
    [Label("边缘格柱子(面 + 格号)")] public List<EdgeCellPillar> edgePillars = new List<EdgeCellPillar>();

    /// <summary>挖掉一块地板的区域(格坐标): 贴到房间边的按"凹面"处理, 不贴边的就是地上的洞</summary>
    [System.Serializable]
    public class RoomRecess
    {
        [Min(0)] [Label("列")] public int col;
        [Min(0)] [Label("行(z从小到大)")] public int row;
        [Min(1)] [Label("宽(列)")] public int cols = 1;
        [Min(1)] [Label("高(行)")] public int rows = 1;
    }

    [InfoBox("这块区域的地板挖掉. 贴到房间边的就是凹面 —— 那一面的栏杆/墙在缺口处跟着凹进去, 沿区域边缘围一圈; 不贴边的只是地上的洞. " +
        "格坐标跟四面栏杆一致: 列从 x 最小端数, 行从 z 最小端数")]
    [Label("凹面/挖洞(格)")] public List<RoomRecess> recesses = new List<RoomRecess>();

    private GameObject FloorPrefab => prefabs.floorPrefab;
    private GameObject SmallFloorPrefab => prefabs.smallFloorPrefab;
    private GameObject RailingPrefab => prefabs.railingPrefab;
    private GameObject PillarPrefab => prefabs.pillarPrefab;
    private GameObject WallPrefab => prefabs.wallPrefab;
    private GameObject OpeningPillarPrefab => prefabs.openingPillarPrefab;

#if UNITY_EDITOR
    [Button("生成房间")]
    public void GenRoom()
    {
        if (prefabs == null)
        {
            Debug.LogError("[RoomLayout] 需要先指定 预制体配置(RoomBlockSo)");
            return;
        }

        if (FloorPrefab == null || PillarPrefab == null || OpeningPillarPrefab == null)
        {
            Debug.LogError("[RoomLayout] 预制体配置里缺 地板/柱子/开口门柱");
            return;
        }

        float cols = Mathf.Max(0.5f, roomSize.x);
        float rows = Mathf.Max(0.5f, roomSize.y);

        // 尺寸带半格的时候, 差的那截要用小地块(1/4 格)补
        bool halfCell = Mathf.Abs(cols - Mathf.Round(cols)) > 0.001f || Mathf.Abs(rows - Mathf.Round(rows)) > 0.001f;
        if (halfCell && SmallFloorPrefab == null)
        {
            Debug.LogError("[RoomLayout] 房间尺寸带 0.5 格, 需要先在配置(RoomBlockSo)里指定 小地块(1/4格)");
            return;
        }

        bool needWall = wallFaces != RoomFace.None || edgeBlocks.Exists(block => block.kind == EdgeBlockKind.Wall);
        if (needWall && WallPrefab == null)
        {
            Debug.LogError("[RoomLayout] 有面要贴墙(或边缘格被指定成墙体), 预制体配置里缺 墙面");
            return;
        }

        if (stairUpFace != RoomFace.None && edgeBlocks.Count > 0)
        {
            Debug.LogWarning("[RoomLayout] 楼梯的面是斜的: 「边缘格围墙」在楼梯上只有 墙体/空 生效(长边那一条斜栏杆不逐格拆)");
        }

        // 没有贴墙的面才需要围栏杆
        Test_WallCell railingCell = null;
        if ((RoomFace.AllFaces & ~wallFaces) != RoomFace.None)
        {
            railingCell = RailingPrefab == null ? null : RailingPrefab.GetComponent<Test_WallCell>();
            if (railingCell == null)
            {
                Debug.LogError("[RoomLayout] 有面要围栏杆, 需要指定带 Test_WallCell 的栏杆预制体(参考 HandrailLayoutCell)");
                return;
            }
        }

        // 单格地板尺寸用与 Test_MapLayout 相同的算法, 保证地板节点的生成结果一致
        Bounds floorBox = GetLayoutBox(FloorPrefab);
        if (Mathf.Abs(floorBox.size.x) < 0.001f || Mathf.Abs(floorBox.size.z) < 0.001f)
        {
            Debug.LogError($"[RoomLayout] 地板预制体 {FloorPrefab.name} 量不出尺寸, 需要带 Mesh 的单格地板");
            return;
        }

        transform.DestroyChildren();

        Transform floorNode = genFloor ? GenFloorNode(rows, cols, floorBox) : null;
        // 房间范围以实际铺出来的地板为准, 避免依赖预制体内部的偏移; 不铺地板(比如楼梯通道)就按单格尺寸推
        Bounds roomArea = floorNode != null ? GetLocalRendererBounds(floorNode) : CalcAreaFromBox(floorBox, cols, rows);
        // 凹面/挖洞: 量完房间范围再挖地板, 免得房间范围跟着被挖小
        if (floorNode != null && recesses.Count > 0)
        {
            CarveFloor(floorNode, roomArea, cols, rows);
        }

        int railingIndex = 0;
        int wallIndex = 0;
        GenRailings(roomArea, cols, rows, railingCell, ref railingIndex);
        GenWalls(roomArea, cols, rows, ref wallIndex);
        GenRecessBounds(roomArea, cols, rows, railingCell, ref railingIndex, ref wallIndex);
        if (genPillars)
        {
            GenPillars(roomArea, cols, rows, GetLayoutBox(PillarPrefab), GetLayoutBox(OpeningPillarPrefab));
            FixPillarsForOpenings(roomArea, cols, rows, GetLayoutBox(PillarPrefab), GetLayoutBox(OpeningPillarPrefab));
            FixPillarsForRecesses(roomArea, cols, rows);
            // 边缘格柱子的指定放最后: 用户点名要的柱子/空 覆盖上面自动摆的结果
            ApplyEdgePillars(roomArea, cols, rows, GetLayoutBox(PillarPrefab), GetLayoutBox(OpeningPillarPrefab));
        }

        Debug.Log($"[RoomLayout] 生成完成: {cols}列 x {rows}行, 贴墙面={wallFaces}, 边缘格围墙 {edgeBlocks.Count} 个, " +
                  $"边缘格柱子 {edgePillars.Count} 个, 凹面/挖洞 {recesses.Count} 个, 房间范围 {roomArea.size}");
    }

    [Button("清理房间")]
    public void ClearRoom()
    {
        transform.DestroyChildren();
    }

    /// <summary>
    /// 铺地板: 整格的部分生成一个 Test_MapLayout 节点(与 FloorRect 里的地板节点一致);
    /// 差的那截(0.5 格)用小地块(1/4 格)补在边上, 数量按边长向上取整、两端对齐, 整条刚好铺满.
    /// </summary>
    private Transform GenFloorNode(float rows, float cols, Bounds floorBox)
    {
        GameObject node = CreateNode(FloorPrefab.name, Vector3.zero, Vector3.zero, Vector3.one);
        int wholeX = Mathf.FloorToInt(cols + 0.001f);
        int wholeZ = Mathf.FloorToInt(rows + 0.001f);

        if (wholeX > 0 && wholeZ > 0)
        {
            Test_MapLayout layout = node.AddComponent<Test_MapLayout>();
            layout.prefab = FloorPrefab;
            layout.genXYZ = new Vector3Int(wholeX, 1, wholeZ); // x列, y层, z行
            layout.addValue = addValue;
            layout.IsHollow = false;
            layout.prefabName = FloorPrefab.name;
            layout.boxSize = floorBox.size;
            layout.center = floorBox.center;
            layout.TestGent();
        }

        if (cols - wholeX > 0.001f || rows - wholeZ > 0.001f)
        {
            // 小块地板单独放一个节点: 整格地板节点自己点"生成/排列"时不会把它们清掉
            GameObject halfNode = CreateNode(FloorPrefab.name + "_Half", Vector3.zero, Vector3.zero, Vector3.one);
            halfNode.transform.SetParent(node.transform, false);
            GenSmallFloor(halfNode.transform, rows, cols, floorBox, wholeX, wholeZ);
        }

        return node.transform;
    }

    /// <summary>
    /// 补差的那 0.5 格地板: 整格铺完后, x 方向差半格就在外侧补一条(铺满整个房间的进深),
    /// z 方向差半格就在外侧补一条(只铺整格那一段, 拐角由前一条盖掉).
    /// </summary>
    private void GenSmallFloor(Transform parent, float rows, float cols, Bounds floorBox, int wholeX, int wholeZ)
    {
        Bounds smallBox = GetLayoutBox(SmallFloorPrefab);
        float unitX = Mathf.Abs(floorBox.size.x);
        float unitZ = Mathf.Abs(floorBox.size.z);
        if (Mathf.Abs(smallBox.size.x) < 0.0001f || Mathf.Abs(smallBox.size.z) < 0.0001f)
        {
            Debug.LogError($"[RoomLayout] 小地块 {SmallFloorPrefab.name} 量不出尺寸, 需要带 Mesh 的小地板");
            return;
        }

        // 地板按"格间距"排(预制体的网格可能朝 -z), 所以整格区域那条边的两个端点在本地坐标里可能是反的
        Vector3 pitch = floorBox.size + addValue;
        float startX = pitch.x >= 0f ? floorBox.center.x - unitX * 0.5f : floorBox.center.x + unitX * 0.5f;   // 整格区域靠原点那一端
        float startZ = pitch.z >= 0f ? floorBox.center.z - unitZ * 0.5f : floorBox.center.z + unitZ * 0.5f;
        float wholeXEnd = startX + wholeX * pitch.x;        // 整格区域铺到哪
        float wholeZEnd = startZ + wholeZ * pitch.z;
        float halfXEnd = wholeXEnd + Mathf.Sign(pitch.x) * (cols - wholeX) * unitX;
        float halfZEnd = wholeZEnd + Mathf.Sign(pitch.z) * (rows - wholeZ) * unitZ;

        if (cols - wholeX > 0.001f)
        {
            GenSmallFloorArea(parent, smallBox,
                Mathf.Min(wholeXEnd, halfXEnd), Mathf.Max(wholeXEnd, halfXEnd),
                Mathf.Min(startZ, halfZEnd), Mathf.Max(startZ, halfZEnd));
        }

        if (rows - wholeZ > 0.001f && wholeX > 0)     // 一格整格都没有时, 上面那条已经铺满整个房间了
        {
            GenSmallFloorArea(parent, smallBox,
                Mathf.Min(startX, wholeXEnd), Mathf.Max(startX, wholeXEnd),
                Mathf.Min(wholeZEnd, halfZEnd), Mathf.Max(wholeZEnd, halfZEnd));
        }
    }

    /// <summary>在一个矩形区域里铺小块地板: 两个方向各自按小块边长算个数(向上取整)再均分, 两端对齐</summary>
    private void GenSmallFloorArea(Transform parent, Bounds smallBox, float xMin, float xMax, float zMin, float zMax)
    {
        float smallX = Mathf.Abs(smallBox.size.x);
        float smallZ = Mathf.Abs(smallBox.size.z);
        float width = Mathf.Max(0f, xMax - xMin);
        float depth = Mathf.Max(0f, zMax - zMin);
        int countX = Mathf.Max(1, Mathf.CeilToInt(width / smallX - 0.001f));
        int countZ = Mathf.Max(1, Mathf.CeilToInt(depth / smallZ - 0.001f));
        float stepX = countX > 1 ? (width - smallX) / (countX - 1) : 0f;
        float stepZ = countZ > 1 ? (depth - smallZ) / (countZ - 1) : 0f;

        for (int i = 0; i < countX; i++)
        {
            for (int j = 0; j < countZ; j++)
            {
                // 摆的位置要让"网格"正好落在这一格上: 原点 = 网格左下角 - (包围盒中心 - 半个尺寸)
                GameObject block = BuildingTool.EditorInstancePrefab(SmallFloorPrefab, parent);
                block.transform.localPosition = new Vector3(
                    xMin + i * stepX - (smallBox.center.x - smallX * 0.5f), 0f,
                    zMin + j * stepZ - (smallBox.center.z - smallZ * 0.5f));
            }
        }
    }

    /// <summary>
    /// 凹面/挖洞: 把落在这块区域里的地板格删掉(地板节点是一整片, 只能把被挖的那几格挑出来删).
    /// 格号按"每一格的包围盒中心"算, 跟四面栏杆用同一套算法 —— 地板预制体的网格朝哪个方向排布都不影响.
    /// </summary>
    private void CarveFloor(Transform floorNode, Bounds area, float cols, float rows)
    {
        float cellX = area.size.x / Mathf.Max(0.5f, cols);
        float cellZ = area.size.z / Mathf.Max(0.5f, rows);
        int cellCols = Mathf.CeilToInt(cols);
        int cellRows = Mathf.CeilToInt(rows);

        for (int i = floorNode.childCount - 1; i >= 0; i--)
        {
            Transform cell = floorNode.GetChild(i);
            Vector3 center = GetRendererCenterLocal(cell);
            int col = Mathf.FloorToInt((center.x - area.min.x) / Mathf.Max(0.0001f, cellX));
            int row = Mathf.FloorToInt((center.z - area.min.z) / Mathf.Max(0.0001f, cellZ));
            if (IsRecessCell(col, row, cellCols, cellRows))
            {
                DestroyImmediate(cell.gameObject);
            }
        }
    }

    /// <summary>取节点(含子物体)的包围盒中心, 换算到本房间的本地空间(跟 roomArea 同一套坐标)</summary>
    private Vector3 GetRendererCenterLocal(Transform node)
    {
        Matrix4x4 toLocal = transform.worldToLocalMatrix;
        bool hasBounds = false;
        Bounds result = new Bounds();

        foreach (Renderer renderer in node.GetComponentsInChildren<Renderer>(true))
        {
            Vector3 center = toLocal.MultiplyPoint3x4(renderer.bounds.center);
            if (!hasBounds)
            {
                result = new Bounds(center, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(center);
            }
        }

        return result.center;
    }

    /// <summary>这个格是不是被凹面/挖洞挖掉了地板</summary>
    private bool IsRecessCell(int col, int row, int cols, int rows)
    {
        foreach (RoomRecess recess in recesses)
        {
            ClampRecess(recess, cols, rows, out int c0, out int r0, out int c1, out int r1);
            if (col >= c0 && col < c1 && row >= r0 && row < r1)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>把一块凹面区域收进房间范围: 取左右/上下边界(左闭右开)</summary>
    private static void ClampRecess(RoomRecess recess, int cols, int rows, out int c0, out int r0, out int c1, out int r1)
    {
        c0 = Mathf.Clamp(recess.col, 0, cols);
        r0 = Mathf.Clamp(recess.row, 0, rows);
        c1 = Mathf.Clamp(recess.col + Mathf.Max(1, recess.cols), c0, cols);
        r1 = Mathf.Clamp(recess.row + Mathf.Max(1, recess.rows), r0, rows);
    }

    /// <summary>该面是否要贴墙</summary>
    private bool HasWallFace(RoomFace face)
    {
        return (wallFaces & face) != RoomFace.None;
    }

    /// <summary>这一格默认放什么(贴墙的面=墙, 其它=栏杆), 再按"边缘格围墙"表里的指定覆盖</summary>
    private EdgeBlockKind GetEdgeKind(RoomFace face, int index)
    {
        foreach (EdgeCellBlock block in edgeBlocks)
        {
            if (block.face == face && block.index == index)
            {
                return block.kind;
            }
        }

        return HasWallFace(face) ? EdgeBlockKind.Wall : EdgeBlockKind.Railing;
    }

    /// <summary>把一段(面上一段连续格)再按每格的类型切小段: 只返回 kind 这一种的小段(起始格号 + 格数)</summary>
    private List<Vector2Int> CalcEdgeKindSpans(RoomFace face, Vector2Int span, EdgeBlockKind kind)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        int start = -1;
        for (int i = 0; i < span.y; i++)
        {
            bool match = GetEdgeKind(face, span.x + i) == kind;
            if (match && start < 0)
            {
                start = span.x + i;
            }

            if (start >= 0 && (!match || i == span.y - 1))
            {
                int end = match ? span.x + i : span.x + i - 1;
                result.Add(new Vector2Int(start, end - start + 1));
                start = -1;
            }
        }

        return result;
    }

    /// <summary>按"面 + 一段格"算出这一段在世界里的起点和长度(最后那格可能是半格, 按面的实际长度截短)</summary>
    private static void CalcFaceRun(FaceInfo info, Vector2Int run, out Vector3 start, out float length)
    {
        float startAlong = Mathf.Min(run.x * info.cellLength, info.faceLength);
        length = Mathf.Min(run.y * info.cellLength, info.faceLength - startAlong);
        start = info.startPos + info.runDir * startAlong;
    }

    /// <summary>楼梯上坡方向是不是沿z(否则沿x)</summary>
    private bool IsStairAlongZ => stairUpFace == RoomFace.ZMin || stairUpFace == RoomFace.ZMax;

    /// <summary>面的排开方向是不是楼梯的上下坡方向(不是的话这个面就是楼梯的上/下端面)</summary>
    private bool IsClimbAlongFaceDir => IsStairAlongZ ? stairUpFace == RoomFace.ZMax : stairUpFace == RoomFace.XMax;

    /// <summary>是不是楼梯的上/下端面</summary>
    private bool IsEndFace(RoomFace face)
    {
        bool faceAlongZ = face == RoomFace.XMin || face == RoomFace.XMax;
        return faceAlongZ != IsStairAlongZ;
    }

    /// <summary>楼梯: 某一个面上第 cellIndex 格的地面高度(相对房间节点)</summary>
    private float CalcStairCellHeight(RoomFace face, int cellIndex, int cellCount)
    {
        if (!IsEndFace(face))
        {
            // 两侧的长边: 从低的那一端数第几格就抬几格
            int fromLow = IsClimbAlongFaceDir ? cellIndex : cellCount - 1 - cellIndex;
            return fromLow * stairRise;
        }

        // 上/下端面: 上坡那一端按最高那一格的踏步算, 另一端在地面
        return face == stairUpFace ? (cellCount - 1) * stairRise : 0f;
    }

    /// <summary>楼梯: 这个面上这一格的构件放在多高(长边取格子中间高度, 端面取踏步高度)</summary>
    private float CalcStairFaceCellY(RoomFace face, int cellIndex, int cellCount)
    {
        float height = CalcStairCellHeight(face, cellIndex, cellCount) + prefabs.stairRailBase;
        return IsEndFace(face) ? height : height + stairRise * 0.5f;
    }

    /// <summary>一个面的排布信息: 起点/朝向/一格长度/格数</summary>
    private struct FaceInfo
    {
        public RoomFace face;
        public Vector3 startPos;
        public Vector3 runDir;
        public Vector3 inwardDir;
        public float yaw;
        public float cellLength;
        public float faceLength;
        public int cellCount;
    }

    /// <summary>取四个面的排布信息(面的起点都是起点格那一角)</summary>
    private FaceInfo[] GetFaceInfos(Bounds area, float cols, float rows)
    {
        float y = area.max.y;
        float cellX = area.size.x / Mathf.Max(0.5f, cols);
        float cellZ = area.size.z / Mathf.Max(0.5f, rows);
        int cellCols = Mathf.CeilToInt(cols);      // 面上的格数: 差的那半格也算一格
        int cellRows = Mathf.CeilToInt(rows);

        return new FaceInfo[]
        {
            new FaceInfo { face = RoomFace.ZMin, startPos = new Vector3(area.min.x, y, area.min.z), runDir = Vector3.right, inwardDir = Vector3.forward, yaw = 0f, cellLength = cellX, faceLength = area.size.x, cellCount = cellCols },
            new FaceInfo { face = RoomFace.ZMax, startPos = new Vector3(area.min.x, y, area.max.z), runDir = Vector3.right, inwardDir = Vector3.back, yaw = 0f, cellLength = cellX, faceLength = area.size.x, cellCount = cellCols },
            new FaceInfo { face = RoomFace.XMin, startPos = new Vector3(area.min.x, y, area.min.z), runDir = Vector3.forward, inwardDir = Vector3.right, yaw = -90f, cellLength = cellZ, faceLength = area.size.z, cellCount = cellRows },
            new FaceInfo { face = RoomFace.XMax, startPos = new Vector3(area.max.x, y, area.min.z), runDir = Vector3.forward, inwardDir = Vector3.left, yaw = -90f, cellLength = cellZ, faceLength = area.size.z, cellCount = cellRows },
        };
    }

    /// <summary>该面上开口占用的格号(升序)</summary>
    private List<int> GetOpeningCells(RoomFace face)
    {
        List<int> cells = new List<int>();
        foreach (FaceOpening opening in openings)
        {
            if (opening.face == face)
            {
                cells.Add(opening.index);
            }
        }

        cells.Sort();
        return cells;
    }

    /// <summary>这个面上要让开(不放栏杆也不放墙)的格: 开口(门洞) + 凹面在这一面上的缺口</summary>
    private List<int> GetEmptyCells(RoomFace face, float cols, float rows)
    {
        List<int> cells = GetOpeningCells(face);
        if (stairUpFace != RoomFace.None)
        {
            return cells;       // 楼梯不做凹面, 缺口也不让开
        }

        int cellCols = Mathf.CeilToInt(cols);
        int cellRows = Mathf.CeilToInt(rows);
        foreach (RoomRecess recess in recesses)
        {
            cells.AddRange(GetRecessMouthCells(recess, face, cellCols, cellRows));
        }

        cells.Sort();
        return cells;
    }

    /// <summary>凹面在这一面上的缺口格号: 没贴着这一面就是空的(左/右面按行号数, 上/下面按列号数)</summary>
    private static List<int> GetRecessMouthCells(RoomRecess recess, RoomFace face, int cols, int rows)
    {
        ClampRecess(recess, cols, rows, out int c0, out int r0, out int c1, out int r1);

        List<int> cells = new List<int>();
        if (face == RoomFace.ZMin && r0 == 0)
        {
            for (int c = c0; c < c1; c++)
            {
                cells.Add(c);
            }
        }
        else if (face == RoomFace.ZMax && r1 == rows)
        {
            for (int c = c0; c < c1; c++)
            {
                cells.Add(c);
            }
        }
        else if (face == RoomFace.XMin && c0 == 0)
        {
            for (int r = r0; r < r1; r++)
            {
                cells.Add(r);
            }
        }
        else if (face == RoomFace.XMax && c1 == cols)
        {
            for (int r = r0; r < r1; r++)
            {
                cells.Add(r);
            }
        }

        return cells;
    }

    /// <summary>把一面切成若干段(去掉开口格), 每段返回 (起始格号, 格数)</summary>
    private static List<Vector2Int> CalcFilledSpans(int cellCount, List<int> openingCells)
    {
        List<Vector2Int> spans = new List<Vector2Int>();
        int i = 0;
        while (i < cellCount)
        {
            if (openingCells.Contains(i))
            {
                i++;
                continue;
            }

            int start = i;
            while (i < cellCount && !openingCells.Contains(i))
            {
                i++;
            }

            spans.Add(new Vector2Int(start, i - start));
        }

        return spans;
    }

    /// <summary>
    /// 围栏杆: 每个面先按"开口 + 凹面缺口"切段, 段内再按每格的围墙类型(墙体/栏杆/空)切小段,
    /// 只给类型是"栏杆"的小段生成 Test_WallLayout 节点(index 与凹面的边接着编).
    /// 楼梯的长边是一整条斜栏杆, 不逐格拆.
    /// </summary>
    private void GenRailings(Bounds area, float cols, float rows, Test_WallCell cell, ref int index)
    {
        foreach (FaceInfo info in GetFaceInfos(area, cols, rows))
        {
            foreach (Vector2Int span in CalcFilledSpans(info.cellCount, GetEmptyCells(info.face, cols, rows)))
            {
                if (stairUpFace == RoomFace.None)
                {
                    foreach (Vector2Int run in CalcEdgeKindSpans(info.face, span, EdgeBlockKind.Railing))
                    {
                        CalcFaceRun(info, run, out Vector3 start, out float length);
                        GenRailingRun(start + info.inwardDir * railingInset, info.yaw, length, index++, cell);
                    }

                    continue;
                }

                if (!IsEndFace(info.face))
                {
                    // 楼梯长边: 一整条斜着的栏杆, 沿上坡方向抬起
                    float angle = Mathf.Atan2(stairRise, info.cellLength) * Mathf.Rad2Deg;
                    float cos = Mathf.Max(0.0001f, Mathf.Cos(angle * Mathf.Deg2Rad));
                    float sin = Mathf.Sin(angle * Mathf.Deg2Rad);
                    bool climbAlongFace = IsClimbAlongFaceDir;
                    float startCell = climbAlongFace ? span.x : span.x + span.y;
                    // 底部贴在踏步面上: 起点格子高度 + 踏步面自身高出预制体原点的那点厚度(见 RoomBlockSo.stairRailBase)
                    float startY = (climbAlongFace ? span.x : info.cellCount - span.x - span.y) * stairRise + prefabs.stairRailBase;

                    // 起点沿坡内缩(只收低端这一头: 起点往里挪、总长相应缩短, 所以高端端头位置不变)
                    float inset = Mathf.Max(0f, prefabs.stairRailStartInset);
                    Vector3 stairPos = info.startPos + info.runDir * (startCell * info.cellLength + inset * cos) + info.inwardDir * railingInset;
                    stairPos.y += startY + inset * sin;

                    float slopeLength = Mathf.Max(0.1f, span.y * info.cellLength / cos - inset);
                    GenRailingRun(stairPos, climbAlongFace ? info.yaw : info.yaw + 180f, slopeLength, index++, cell, angle);
                    continue;
                }

                // 楼梯端面: 按每一格的踏步高度放(逐格也支持改类型)
                foreach (Vector2Int run in CalcEdgeKindSpans(info.face, span, EdgeBlockKind.Railing))
                {
                    CalcFaceRun(info, run, out Vector3 start, out float length);
                    Vector3 endPos = start + info.inwardDir * railingInset;
                    endPos.y += CalcStairFaceCellY(info.face, run.x, info.cellCount);
                    GenRailingRun(endPos, info.yaw, length, index++, cell);
                }
            }
        }
    }

    /// <summary>
    /// 贴墙: 每个面先按"开口 + 凹面缺口"切段, 段内再按每格的围墙类型切小段,
    /// 只给类型是"墙体"的小段生成 Test_MapLayout 节点(index 与凹面的边接着编).
    /// </summary>
    private void GenWalls(Bounds area, float cols, float rows, ref int index)
    {
        // 一面墙都没有(默认都是栏杆, 边缘格也没指定成墙体)就不用生成
        bool needWall = wallFaces != RoomFace.None || edgeBlocks.Exists(block => block.kind == EdgeBlockKind.Wall);
        if (!needWall)
        {
            return;
        }

        Bounds wallBox = GetLayoutBox(WallPrefab);
        foreach (FaceInfo info in GetFaceInfos(area, cols, rows))
        {
            foreach (Vector2Int span in CalcFilledSpans(info.cellCount, GetEmptyCells(info.face, cols, rows)))
            {
                if (stairUpFace == RoomFace.None)
                {
                    foreach (Vector2Int run in CalcEdgeKindSpans(info.face, span, EdgeBlockKind.Wall))
                    {
                        CalcFaceRun(info, run, out Vector3 start, out float length);
                        GenWallRun(start, info.yaw, length, index++, wallBox);
                    }

                    continue;
                }

                // 楼梯: 每格一段墙, 跟着踏步逐格抬高
                for (int c = 0; c < span.y; c++)
                {
                    int cellIndex = span.x + c;
                    if (GetEdgeKind(info.face, cellIndex) != EdgeBlockKind.Wall)
                    {
                        continue;       // 这一格被改成栏杆/空了
                    }

                    Vector3 pos = info.startPos + info.runDir * (cellIndex * info.cellLength);
                    pos.y += CalcStairFaceCellY(info.face, cellIndex, info.cellCount);
                    float cellLength = Mathf.Min(info.cellLength, info.faceLength - cellIndex * info.cellLength);
                    GenWallRun(pos, info.yaw, cellLength, index++, wallBox);
                }
            }
        }
    }

    /// <summary>生成一面墙: 墙段沿节点本地+x排开(yaw控制墙面朝向), 按边的长度均分, 保证两端对齐</summary>
    private void GenWallRun(Vector3 startPos, float yaw, float runLength, int index, Bounds wallBox)
    {
        int count = Mathf.Max(1, Mathf.RoundToInt(runLength / Mathf.Max(0.001f, Mathf.Abs(wallBox.size.x))));
        float step = runLength / count;

        GameObject node = CreateNode(IndexedName(WallPrefab.name, index), startPos, new Vector3(0f, yaw, 0f), Vector3.one);
        node.layer = Layers.WALL_BLOCK;

        Test_MapLayout layout = node.AddComponent<Test_MapLayout>();
        layout.prefab = WallPrefab;
        layout.genXYZ = new Vector3Int(count, 1, 1);
        layout.addValue = new Vector3(step - wallBox.size.x, 0f, 0f);
        layout.IsHollow = false;
        layout.prefabName = WallPrefab.name;
        layout.boxSize = wallBox.size;
        layout.center = wallBox.center;
        layout.TestGent();

        if (addWallCollider)
        {
            BuildingTool.AddBoxCollider(node.transform);
        }
    }

    /// <summary>生成一段栏杆, runLength为这段的世界长度; tiltAngle是楼梯段沿上坡方向的倾斜角</summary>
    private void GenRailingRun(Vector3 nodePos, float yaw, float runLength, int index, Test_WallCell cell, float tiltAngle = 0f)
    {
        GameObject node = CreateNode(IndexedName(RailingPrefab.name.Replace("Cell", ""), index), nodePos,
            new Vector3(0f, yaw, tiltAngle), Vector3.one * railingScale);

        Test_WallLayout layout = node.AddComponent<Test_WallLayout>();
        layout.prefab = cell;
        // 按距离生成: 段数取整后, 差的那截长度靠削减(或压紧) AA 重叠摊掉, 整段刚好铺满这条边
        layout.distance = Mathf.Abs(runLength);
        layout.GenByDistance();

        if (addWallCollider)
        {
            BuildingTool.AddBoxCollider(node.transform);
        }
    }

    /// <summary>
    /// 凹面: 贴着房间边的挖洞, 那一面的边界在缺口处往里凹 —— 沿这块区域剩下几条边(边缘外面还有地板的那几条)补上那一面的构件,
    /// 缺口那条边(原来的面线)不放东西, 看起来就是房间边界凹进去一块. 不贴边的区域不在这里补, 它只是地上的洞.
    /// 楼梯的面是斜的, 不做凹面.
    /// </summary>
    private void GenRecessBounds(Bounds area, float cols, float rows, Test_WallCell cell, ref int railingIndex, ref int wallIndex)
    {
        if (recesses.Count == 0 || stairUpFace != RoomFace.None)
        {
            return;
        }

        float cellX = area.size.x / Mathf.Max(0.5f, cols);
        float cellZ = area.size.z / Mathf.Max(0.5f, rows);
        int cellCols = Mathf.CeilToInt(cols);
        int cellRows = Mathf.CeilToInt(rows);
        Bounds wallBox = wallFaces != RoomFace.None ? GetLayoutBox(WallPrefab) : default;

        foreach (RoomRecess recess in recesses)
        {
            ClampRecess(recess, cellCols, cellRows, out int c0, out int r0, out int c1, out int r1);
            bool touchXMin = c0 == 0;
            bool touchXMax = c1 == cols;
            bool touchZMin = r0 == 0;
            bool touchZMax = r1 == rows;
            if (!touchXMin && !touchXMax && !touchZMin && !touchZMax)
            {
                continue;   // 不贴房间边: 只是地上的洞, 不用围
            }

            // 沿x的边(平行于上/下面)按 Z 面算, 沿z的边(平行于左/右面)按 X 面算;
            // 那个面没被这块区域碰到时(凹面只贴一条边时的那两条侧边), 就跟它贴着的那一面一样
            RoomFace faceAlongX = touchZMin ? RoomFace.ZMin
                : touchZMax ? RoomFace.ZMax : (touchXMin ? RoomFace.XMin : RoomFace.XMax);
            RoomFace faceAlongZ = touchXMin ? RoomFace.XMin
                : touchXMax ? RoomFace.XMax : (touchZMin ? RoomFace.ZMin : RoomFace.ZMax);

            float x0 = area.min.x + c0 * cellX;
            float x1 = area.min.x + c1 * cellX;
            float z0 = area.min.z + r0 * cellZ;
            float z1 = area.min.z + r1 * cellZ;
            float y = area.max.y;
            float spanX = (c1 - c0) * cellX;        // 沿x的边长
            float spanZ = (r1 - r0) * cellZ;        // 沿z的边长

            // 四边里贴在房间边上的那条是缺口(朝外), 不放东西; 其余各补一条(栏杆朝有地板的一侧内缩)
            if (!touchZMin)
            {
                GenRecessEdge(faceAlongX, true, new Vector3(x0, y, z0), spanX, Vector3.back, cell, wallBox, ref railingIndex, ref wallIndex);
            }
            if (!touchZMax)
            {
                GenRecessEdge(faceAlongX, true, new Vector3(x0, y, z1), spanX, Vector3.forward, cell, wallBox, ref railingIndex, ref wallIndex);
            }
            if (!touchXMin)
            {
                GenRecessEdge(faceAlongZ, false, new Vector3(x0, y, z0), spanZ, Vector3.left, cell, wallBox, ref railingIndex, ref wallIndex);
            }
            if (!touchXMax)
            {
                GenRecessEdge(faceAlongZ, false, new Vector3(x1, y, z0), spanZ, Vector3.right, cell, wallBox, ref railingIndex, ref wallIndex);
            }
        }
    }

    /// <summary>凹面的一条边: 沿 +x(alongX)或 +z 从 start 摆 length; inward 是有地板的那一侧</summary>
    private void GenRecessEdge(RoomFace face, bool alongX, Vector3 start, float length, Vector3 inward,
        Test_WallCell cell, Bounds wallBox, ref int railingIndex, ref int wallIndex)
    {
        float yaw = alongX ? 0f : -90f;

        // 跟它贴着的那一面一致: 那一面是墙就补墙, 是栏杆就补栏杆
        if (HasWallFace(face))
        {
            GenWallRun(start, yaw, length, wallIndex++, wallBox);
            return;
        }

        GenRailingRun(start + inward * railingInset, yaw, length, railingIndex++, cell);
    }

    /// <summary>取预制体在排版空间里的单格尺寸(与 Test_MapLayout 的"计算"一致, 会把预制体自身的旋转算进去)</summary>
    public static Bounds GetLayoutBox(GameObject prefab)
    {
        Bounds bounds = BuildingTool.GetPrefabBounds(prefab);
        if (!prefab.transform.localEulerAngles.IsZore())
        {
            bounds.size = prefab.transform.rotation * bounds.size;
            bounds.center = prefab.transform.rotation * bounds.center;
        }

        return bounds;
    }

    /// <summary>
    /// 沿四条边摆柱子: 四个角落只摆一次.
    /// 配置里勾了"是否使用开口门柱"时, 四个角落用普通柱子, 每条边中间的柱子换成开口门柱.
    /// </summary>
    private void GenPillars(Bounds area, float cols, float rows, Bounds pillarBox, Bounds openingPillarBox)
    {
        float y = area.max.y + pillarY;

        // 按格数算区间数, 再按边长均匀分配, 保证两端角落都有柱子且间距不超过设定间隔
        int segmentsX = CalcPillarSegments(cols, pillarInterval);
        int segmentsZ = CalcPillarSegments(rows, pillarInterval);
        int countX = segmentsX + 1;
        int countZ = segmentsZ + 1;
        float stepX = area.size.x / segmentsX;
        float stepZ = area.size.z / segmentsZ;

        float zMin = area.min.z + pillarInset;
        float zMax = area.max.z - pillarInset;
        float xMinSide = area.min.x + pillarInset;
        float xMaxSide = area.max.x - pillarInset;
        int index = 0;

        if (!prefabs.edgeMiddleUseOpeningPillar)
        {
            // 默认: 每条边一整排普通柱子(含两端的角落)
            Vector3Int genX = new Vector3Int(countX, 1, 1);
            Vector3 spacingX = new Vector3(stepX - pillarBox.size.x, 0f, 0f);
            GenPillarRun(new Vector3(area.min.x, y, zMin), genX, spacingX, PillarPrefab, pillarBox, index++);
            GenPillarRun(new Vector3(area.min.x, y, zMax), genX, spacingX, PillarPrefab, pillarBox, index++);

            // 沿z的两条边跳过两端角落, 角落由上面的两条边负责
            if (countZ > 2)
            {
                Vector3Int genZ = new Vector3Int(1, 1, countZ - 2);
                Vector3 spacingZ = new Vector3(0f, 0f, stepZ - pillarBox.size.z);
                GenPillarRun(new Vector3(xMinSide, y, area.min.z + stepZ), genZ, spacingZ, PillarPrefab, pillarBox, index++);
                GenPillarRun(new Vector3(xMaxSide, y, area.min.z + stepZ), genZ, spacingZ, PillarPrefab, pillarBox, index++);
            }

            return;
        }

        // 勾选后: 四个角落仍是普通柱子
        Vector3Int single = Vector3Int.one;
        GenPillarRun(new Vector3(area.min.x, y, zMin), single, Vector3.zero, PillarPrefab, pillarBox, index++);
        GenPillarRun(new Vector3(area.max.x, y, zMin), single, Vector3.zero, PillarPrefab, pillarBox, index++);
        GenPillarRun(new Vector3(area.min.x, y, zMax), single, Vector3.zero, PillarPrefab, pillarBox, index++);
        GenPillarRun(new Vector3(area.max.x, y, zMax), single, Vector3.zero, PillarPrefab, pillarBox, index++);

        // 沿x的两条边: 去掉两端角落, 中间几根换成开口门柱
        if (countX > 2)
        {
            Vector3Int genX = new Vector3Int(countX - 2, 1, 1);
            Vector3 spacingX = new Vector3(stepX - openingPillarBox.size.x, 0f, 0f);
            GenPillarRun(new Vector3(area.min.x + stepX, y, zMin), genX, spacingX, OpeningPillarPrefab, openingPillarBox, index++);
            GenPillarRun(new Vector3(area.min.x + stepX, y, zMax), genX, spacingX, OpeningPillarPrefab, openingPillarBox, index++);
        }

        // 沿z的两条边本来就不含角落, 整条换成开口门柱
        if (countZ > 2)
        {
            Vector3Int genZ = new Vector3Int(1, 1, countZ - 2);
            Vector3 spacingZ = new Vector3(0f, 0f, stepZ - openingPillarBox.size.z);
            GenPillarRun(new Vector3(xMinSide, y, area.min.z + stepZ), genZ, spacingZ, OpeningPillarPrefab, openingPillarBox, index++);
            GenPillarRun(new Vector3(xMaxSide, y, area.min.z + stepZ), genZ, spacingZ, OpeningPillarPrefab, openingPillarBox, index++);
        }
    }

    /// <summary>
    /// 生成一条边上的一排柱子. spacingValue 用来把 Test_MapLayout 的间距调整成 stepX/stepZ;
    /// pillarPrefab 是这条边实际要用的柱子(普通柱子 / 开口门柱), pillarBox 必须和它对应.
    /// </summary>
    private void GenPillarRun(Vector3 startPos, Vector3Int genXYZ, Vector3 spacingValue, GameObject pillarPrefab, Bounds pillarBox, int index)
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
    private static int CalcPillarSegments(float cellCount, float intervalCells)
    {
        return Mathf.Max(1, Mathf.CeilToInt(cellCount / Mathf.Max(0.001f, intervalCells) - 0.001f));
    }

    /// <summary>
    /// 边缘格柱子覆盖: 按 面 + 格号 把这一格的柱子换成指定类型.
    /// 这一格原本有柱子就"原地换一根"(位置保持不动); 本来没有才补一根, 补在这一格的格线上(格子起点, 不是格子中间); 空 = 只清不加.
    /// </summary>
    private void ApplyEdgePillars(Bounds area, float cols, float rows, Bounds pillarBox, Bounds openingPillarBox)
    {
        if (edgePillars.Count == 0 || stairUpFace != RoomFace.None)
        {
            return;     // 楼梯不摆柱子, 也不做覆盖
        }

        float y = area.max.y + pillarY;
        List<Transform> pillarRuns = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (IsPillarNode(child))
            {
                pillarRuns.Add(child);
            }
        }

        int index = 0;
        foreach (FaceInfo info in GetFaceInfos(area, cols, rows))
        {
            foreach (EdgeCellPillar edge in edgePillars)
            {
                if (edge.face != info.face)
                {
                    continue;
                }

                float cellStart = Mathf.Min(edge.index * info.cellLength, info.faceLength);
                float cellEnd = Mathf.Min(cellStart + info.cellLength, info.faceLength);

                bool openingKind = edge.kind == EdgePillarKind.OpeningPillar;
                GameObject prefab = openingKind ? OpeningPillarPrefab : PillarPrefab;
                Bounds box = openingKind ? openingPillarBox : pillarBox;

                // 这一格原来有几根就处理几根: 空 -> 清掉; 换成柱子 -> 原地换一根(位置不动)
                bool found = false;
                foreach (Transform run in pillarRuns)
                {
                    for (int k = run.childCount - 1; k >= 0; k--)
                    {
                        Vector3 pos = run.localPosition + run.GetChild(k).localPosition;
                        if (!IsInEdgeCell(pos, info, cellStart, cellEnd))
                        {
                            continue;
                        }

                        found = true;
                        DestroyImmediate(run.GetChild(k).gameObject);
                        if (edge.kind != EdgePillarKind.Empty)
                        {
                            Vector3 keep = pos;
                            keep.y = y;
                            GenPillarRun(keep, Vector3Int.one, Vector3.zero, prefab, box, index++);
                        }
                    }
                }

                if (found || edge.kind == EdgePillarKind.Empty)
                {
                    continue;       // 这一格原来就有柱子(已原地换完), 或者要的就是空
                }

                // 这一格本来没有柱子: 补一根在格线上(格子起点那一侧), 不放格子中间
                Vector3 target = info.startPos + info.runDir * cellStart + info.inwardDir * pillarInset;
                target.y = y;
                if (!HasPillarAt(target))
                {
                    GenPillarRun(target, Vector3Int.one, Vector3.zero, prefab, box, index++);
                }
            }
        }
    }

    /// <summary>开口处理: 清掉落在开口里的柱子, 再按每个开口指定的类型在两侧补柱子(开口柱子/柱子/空)</summary>
    private void FixPillarsForOpenings(Bounds area, float cols, float rows, Bounds pillarBox, Bounds openingPillarBox)
    {
        if (openings.Count == 0)
        {
            return;
        }

        float y = area.max.y + pillarY;
        List<Transform> pillarRuns = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (IsPillarNode(child))
            {
                pillarRuns.Add(child);
            }
        }

        int index = 0;
        foreach (FaceInfo info in GetFaceInfos(area, cols, rows))
        {
            foreach (int cellIndex in GetOpeningCells(info.face))
            {
                float from = cellIndex * info.cellLength;
                float to = from + info.cellLength;

                // 开口范围内原本的柱子要去掉, 不然门口杵着一根柱子
                foreach (Transform run in pillarRuns)
                {
                    for (int k = run.childCount - 1; k >= 0; k--)
                    {
                        Vector3 pos = run.localPosition + run.GetChild(k).localPosition;
                        if (IsOnFace(pos, info, from, to))
                        {
                            DestroyImmediate(run.GetChild(k).gameObject);
                        }
                    }
                }

                OpeningPillarKind kind = openings.Find(x => x.face == info.face && x.index == cellIndex).pillar;
                if (kind == OpeningPillarKind.Empty)
                {
                    continue;
                }

                bool openingPillar = kind == OpeningPillarKind.OpeningPillar;
                GenOpeningPillar(info, from, y, ref index, openingPillar ? OpeningPillarPrefab : PillarPrefab,
                    openingPillar ? openingPillarBox : pillarBox);
                GenOpeningPillar(info, to, y, ref index, openingPillar ? OpeningPillarPrefab : PillarPrefab,
                    openingPillar ? openingPillarBox : pillarBox);
            }
        }
    }

    /// <summary>凹面/挖洞里的柱子会悬空, 去掉: 按柱子所在格判断</summary>
    private void FixPillarsForRecesses(Bounds area, float cols, float rows)
    {
        if (recesses.Count == 0 || stairUpFace != RoomFace.None)
        {
            return;     // 楼梯不做凹面
        }

        float cellX = area.size.x / Mathf.Max(0.5f, cols);
        float cellZ = area.size.z / Mathf.Max(0.5f, rows);
        int cellCols = Mathf.CeilToInt(cols);
        int cellRows = Mathf.CeilToInt(rows);

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform run = transform.GetChild(i);
            if (!IsPillarNode(run))
            {
                continue;
            }

            for (int k = run.childCount - 1; k >= 0; k--)
            {
                Vector3 pos = run.localPosition + run.GetChild(k).localPosition;
                int col = Mathf.Clamp(Mathf.FloorToInt((pos.x - area.min.x) / Mathf.Max(0.0001f, cellX)), 0, cellCols - 1);
                int row = Mathf.Clamp(Mathf.FloorToInt((pos.z - area.min.z) / Mathf.Max(0.0001f, cellZ)), 0, cellRows - 1);
                if (IsRecessCell(col, row, cellCols, cellRows))
                {
                    DestroyImmediate(run.GetChild(k).gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 柱子在不在这一格上(同一条边, 且沿边方向落在这一格里, 含格子两条边线上那两颗).
    /// 跟 IsOnFace 的区别: 边线上(格线)的柱子也算这一格的, 这样角落那颗才认得到。
    /// </summary>
    private bool IsInEdgeCell(Vector3 localPos, FaceInfo info, float from, float to)
    {
        Vector3 rel = localPos - info.startPos;
        Vector3 offset = rel - Vector3.Project(rel, info.runDir);
        float along = Vector3.Dot(rel, info.runDir);
        return Mathf.Abs(offset.y) < 1f &&
               offset.magnitude < Mathf.Abs(pillarInset) + 0.6f &&
               along >= from - 0.05f && along <= to + 0.05f;
    }

    /// <summary>柱子在不在某面的开口范围内(同一条边, 且沿边方向落在开口里)</summary>
    private bool IsOnFace(Vector3 localPos, FaceInfo info, float from, float to)
    {
        Vector3 rel = localPos - info.startPos;
        Vector3 offset = rel - Vector3.Project(rel, info.runDir);
        float along = Vector3.Dot(rel, info.runDir);
        return Mathf.Abs(offset.y) < 1f &&
               offset.magnitude < Mathf.Abs(pillarInset) + 0.6f &&
               along > from + 0.05f && along < to - 0.05f;
    }

    /// <summary>在面的 along 处补一根柱子(这个位置本来就有柱子则跳过); prefab/box 决定用哪种柱子</summary>
    private void GenOpeningPillar(FaceInfo info, float along, float y, ref int index, GameObject pillarPrefab, Bounds pillarBox)
    {
        Vector3 pos = info.startPos + info.runDir * along;
        pos.y = y;
        if (HasPillarAt(pos))
        {
            return;
        }

        GameObject node = CreateNode(IndexedName(pillarPrefab.name + "_Door", index++), pos, new Vector3(0f, info.yaw, 0f), Vector3.one);

        Test_MapLayout layout = node.AddComponent<Test_MapLayout>();
        layout.prefab = pillarPrefab;
        layout.genXYZ = Vector3Int.one;
        layout.addValue = Vector3.zero;
        layout.IsHollow = false;
        layout.prefabName = pillarPrefab.name;
        layout.boxSize = pillarBox.size;
        layout.center = pillarBox.center;
        layout.TestGent();
    }

    /// <summary>这个位置是不是已经有柱子了(和已生成的柱子比位置)</summary>
    private bool HasPillarAt(Vector3 localPos)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!IsPillarNode(child))
            {
                continue;
            }

            foreach (Transform pillar in child)
            {
                if ((child.localPosition + pillar.localPosition - localPos).sqrMagnitude < 0.09f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>是不是柱子的布局节点(普通柱子 / 开口门柱)</summary>
    private bool IsPillarNode(Transform child)
    {
        if (child.GetComponent<Test_MapLayout>() == null)
        {
            return false;
        }

        return child.name.StartsWith(PillarPrefab.name) || child.name.StartsWith(OpeningPillarPrefab.name);
    }

    /// <summary>不铺地板时: 按单格尺寸推出房间范围(和有地板时量出来的范围一致)</summary>
    private static Bounds CalcAreaFromBox(Bounds floorBox, float cols, float rows)
    {
        Vector3 min = new Vector3(Mathf.Min(0f, floorBox.size.x * cols), floorBox.min.y, Mathf.Min(0f, floorBox.size.z * rows));
        Vector3 max = new Vector3(Mathf.Max(0f, floorBox.size.x * cols), floorBox.max.y, Mathf.Max(0f, floorBox.size.z * rows));
        return new Bounds((min + max) * 0.5f, max - min);
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
    public static string IndexedName(string baseName, int index)
    {
        return index == 0 ? baseName : $"{baseName} ({index})";
    }
#endif
}
