#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 房间拼装编辑器: 在格网画布上拖画房间/通道, 生成到 Test_DungeonLayout 节点上.
/// 输入就是"画" —— 拉出房间矩形、拖出通道; 房间之间边贴边就会自动开口当门, 开口两侧补柱子.
/// </summary>
public class Test_DungeonLayoutWindow : EditorWindow
{
    private enum DrawTool
    {
        Select,
        Room,
        Corridor,
        Erase
    }

    private const string DefaultSettingPath = "Assets/_Res/Item/Static/room/RoomBlockSo.asset";

    private static readonly Color GridColor = new Color(0.3f, 0.3f, 0.3f, 0.55f);
    private static readonly Color GridMajorColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    private static readonly Color RoomFill = new Color(0.35f, 0.55f, 0.78f, 0.35f);
    private static readonly Color CorridorFill = new Color(0.45f, 0.78f, 0.55f, 0.35f);
    private static readonly Color StairFill = new Color(0.85f, 0.65f, 0.35f, 0.40f);
    private static readonly Color PieceBorder = new Color(0.72f, 0.78f, 0.85f, 0.95f);
    private static readonly Color SelectBorder = new Color(1f, 0.72f, 0.2f, 1f);
    private static readonly Color DoorColor = new Color(1f, 0.85f, 0.3f, 1f);
    private static readonly Color DragFill = new Color(0.95f, 0.9f, 0.5f, 0.25f);
    private static readonly Color RecessFill = new Color(0.08f, 0.08f, 0.1f, 0.65f);
    private static readonly Color RecessBorder = new Color(0.55f, 0.7f, 0.95f, 0.95f);
    private static readonly Color HalfGridColor = new Color(0.26f, 0.26f, 0.26f, 0.5f);
    private static readonly Color WallMarkColor = new Color(0.88f, 0.88f, 0.95f, 1f);
    private static readonly Color RailingMarkColor = new Color(0.45f, 0.8f, 1f, 1f);
    private static readonly Color EmptyMarkColor = new Color(0.9f, 0.35f, 0.35f, 1f);
    private static readonly Color PillarMarkColor = new Color(0.55f, 0.95f, 0.55f, 1f);

    private Test_DungeonLayout target;
    private DrawTool tool = DrawTool.Select;
    private float cellPx = 26f;
    private Vector2 scroll;
    private bool autoGen = true;
    private int selected = -1;
    private bool dragging;
    private Vector2Int dragStartCell;       // 半格坐标(0.5 格 = 1), 下面这些格坐标都是半格
    private Vector2Int dragEndCell;
    private int movingPiece = -1;
    private Vector2Int moveGrabCell;
    private Vector2Int moveOriginCell;
    private Vector2Int movePreviewCell;
    private bool selectAll;                                     // 选择模式按 A: 全选, 之后拖拽是整体平移
    private bool movingAll;
    private readonly List<Vector2Int> moveOrigins = new List<Vector2Int>();
    private Vector2Int moveDelta;
    private Vector2 mouseWindowPos;
    private GUIStyle pieceLabelStyle;

    // 底部面板各模块的折叠状态
    private bool showPieceBasic = true;
    private bool showEdgeBlocks = true;
    private bool showEdgePillars = true;
    private bool showRecesses = true;

    /// <summary>模块标题占的宽度: 加号跟在这后面, 各模块的加号因此是对齐的</summary>
    private const float HeaderTitleWidth = 132f;

    /// <summary>画布上 1 格 = cellPx 像素, 半个格 = halfPx</summary>
    private float halfPx => cellPx * 0.5f;

    /// <summary>格(可以是 0.5 的倍数) -> 半格整数, 跟 Test_DungeonLayout 的占用表同一套单位</summary>
    private static int ToHalf(float cells) => Test_DungeonLayout.ToHalf(cells);

    /// <summary>半格整数 -> 格</summary>
    private static float Cells(int half) => Test_DungeonLayout.FromHalf(half);

    /// <summary>格的数字: 整数不带小数点(2), 半格写 1.5</summary>
    private static string FormatCells(float cells)
    {
        return Mathf.Abs(cells - Mathf.Round(cells)) < 0.001f ? Mathf.RoundToInt(cells).ToString() : cells.ToString("0.#");
    }

    [MenuItem("Tools/房间拼装编辑器")]
    private static void Open()
    {
        Open(null);
    }

    /// <summary>打开编辑器; layout 为空时用当前选中的节点</summary>
    public static void Open(Test_DungeonLayout layout)
    {
        Test_DungeonLayoutWindow window = GetWindow<Test_DungeonLayoutWindow>("房间拼装");
        window.minSize = new Vector2(760f, 560f);
        if (layout != null)
        {
            window.target = layout;
        }
        else
        {
            window.TryUseSelection(false);
        }

        window.Show();
    }

    private void OnEnable()
    {
        TryUseSelection(false);
    }

    private void OnSelectionChange()
    {
        if (target == null)
        {
            TryUseSelection(false);
        }

        Repaint();
    }

    private void OnGUI()
    {
        // 在任何 GUI 组/裁剪之前抓一次鼠标位置, 此时一定是窗口坐标, 换算成画布坐标才不会被裁剪矩阵影响
        mouseWindowPos = Event.current.mousePosition;

        HandleKeyEvent();

        if (pieceLabelStyle == null)
        {
            pieceLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.95f, 1f) }
            };
        }

        DrawHeader();

        if (target == null)
        {
            EditorGUILayout.HelpBox("先选中场景里带 Test_DungeonLayout 的节点, 或者点「新建布局节点」。", MessageType.Info);
            return;
        }

        DrawToolbar();
        DrawCanvas();
        DrawFooter();
    }

    /// <summary>Del 键删除选中的房间/通道(正在输入框里打字时不响应)</summary>
    private void HandleKeyEvent()
    {
        Event current = Event.current;
        if (current.type != EventType.KeyDown)
        {
            return;
        }

        // 只有"正在输入框里打字"时才让开; 下拉/普通控件残留的焦点不影响 Del
        if (EditorGUIUtility.editingTextField)
        {
            return;
        }

        if (current.keyCode == KeyCode.Delete)
        {
            // 全选状态下不删(Del 只删单个选中的); 想清空用下面的"清空布局"
            if (!selectAll && target != null && selected >= 0 && selected < target.pieces.Count)
            {
                RemovePiece(selected);
            }

            current.Use();
            return;
        }

        if (current.keyCode == KeyCode.A && tool == DrawTool.Select && target != null && target.pieces.Count > 0)
        {
            // 选择模式按 A: 全选, 接着拖拽就是整体平移
            selectAll = true;
            selected = -1;
            Repaint();
            current.Use();
            return;
        }

        if (current.keyCode == KeyCode.Escape)
        {
            selectAll = false;
            selected = -1;
            Repaint();
            current.Use();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        target = (Test_DungeonLayout)EditorGUILayout.ObjectField("布局节点", target, typeof(Test_DungeonLayout), true);
        if (GUILayout.Button("用选中节点", GUILayout.Width(90f)))
        {
            TryUseSelection(true);
        }

        if (GUILayout.Button("新建布局节点", GUILayout.Width(100f)))
        {
            CreateLayoutNode();
        }

        if (target != null && GUILayout.Button("补默认配置", GUILayout.Width(90f)))
        {
            FillDefaultSetting();
        }

        EditorGUILayout.EndHorizontal();

        if (target != null && target.prefabs == null)
        {
            EditorGUILayout.HelpBox("还没指定「预制体配置」, 点「补默认配置」或手动拖 RoomBlockSo 资产进去。", MessageType.Warning);
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        tool = (DrawTool)GUILayout.Toolbar((int)tool, new string[] { "选择", "画房间", "画通道", "擦除" },
            EditorStyles.toolbarButton, GUILayout.Width(260f));

        GUILayout.Label("格子大小", EditorStyles.miniLabel, GUILayout.Width(56f));
        cellPx = GUILayout.HorizontalSlider(cellPx, 10f, 46f, GUILayout.Width(110f));

        bool newAutoGen = GUILayout.Toggle(autoGen, "自动生成", EditorStyles.toolbarButton, GUILayout.Width(70f));
        if (newAutoGen != autoGen)
        {
            autoGen = newAutoGen;
            if (autoGen)
            {
                Apply();
            }
        }

        if (GUILayout.Button("生成", EditorStyles.toolbarButton, GUILayout.Width(48f)))
        {
            target.GenAll();
            MarkDirty();
        }

        if (GUILayout.Button("清理生成物", EditorStyles.toolbarButton, GUILayout.Width(78f)))
        {
            Undo.RecordObject(target, "清理生成物");
            target.ClearAll();
            MarkDirty();
            Repaint();
        }

        if (GUILayout.Button("自动居中", EditorStyles.toolbarButton, GUILayout.Width(64f)))
        {
            AutoCenter();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("「选择」点一个房间/通道可改尺寸/位置/墙面/楼梯方向, 按住左键拖就能搬动(按 A 全选后拖=整体平移); 画房间/画通道时点在已有的块上会改为选中; 选中后按 Del 删除; 边贴边会自动开口当门并补柱子; 房间还能加「凹面/挖洞」(把一块地板挖掉, 贴边的围一圈); 网格按半格, 尺寸/位置可以画到 0.5 格(楼梯/门洞仍按整格); 某一格想单独改围墙(墙/栏杆/空)或柱子(柱子/开口柱子/空)就在下面加一条。",
            EditorStyles.miniLabel);
    }

    /// <summary>把所有房间/通道整体平移, 让它们包围盒的中心落在原点(格子 0,0)</summary>
    private void AutoCenter()
    {
        if (target.pieces.Count == 0)
        {
            return;
        }

        int minCol = int.MaxValue;
        int minRow = int.MaxValue;
        int maxCol = int.MinValue;
        int maxRow = int.MinValue;
        foreach (DungeonPiece piece in target.pieces)
        {
            minCol = Mathf.Min(minCol, ToHalf(piece.col));
            minRow = Mathf.Min(minRow, ToHalf(piece.row));
            maxCol = Mathf.Max(maxCol, ToHalf(piece.col + piece.cols));
            maxRow = Mathf.Max(maxRow, ToHalf(piece.row + piece.rows));
        }

        int shiftX = Mathf.RoundToInt(-(minCol + maxCol) * 0.5f);
        int shiftY = Mathf.RoundToInt(-(minRow + maxRow) * 0.5f);
        if (shiftX == 0 && shiftY == 0)
        {
            return;
        }

        Undo.RecordObject(target, "整体居中");
        foreach (DungeonPiece piece in target.pieces)
        {
            piece.col = Cells(ToHalf(piece.col) + shiftX);
            piece.row = Cells(ToHalf(piece.row) + shiftY);
        }

        Apply();
    }

    private void DrawCanvas()
    {
        Rect viewRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(viewRect, new Color(0.18f, 0.18f, 0.18f, 1f));

        Vector2Int origin = CalcGridOrigin();
        Vector2Int size = CalcGridSize(origin);
        Rect contentRect = new Rect(0f, 0f, size.x * halfPx + 2f, size.y * halfPx + 2f);

        scroll = GUI.BeginScrollView(viewRect, scroll, contentRect);

        DrawGrid(size);
        DrawOriginMarker(origin);
        DrawPieces(origin);
        DrawDraggingPiece(origin);
        HandleCanvasEvent(viewRect, contentRect, origin, size);

        if (movingPiece >= 0 || movingAll)
        {
            EditorGUIUtility.AddCursorRect(viewRect, MouseCursor.MoveArrow);
        }

        GUI.EndScrollView();
    }

    private void DrawGrid(Vector2Int size)
    {
        // size 是半格数: 半格线浅一点, 整格线深一点, 5 格线最深
        for (int x = 0; x <= size.x; x++)
        {
            Color color = x % 10 == 0 ? GridMajorColor : (x % 2 == 0 ? GridColor : HalfGridColor);
            EditorGUI.DrawRect(new Rect(x * halfPx, 0f, 1f, size.y * halfPx), color);
        }

        for (int y = 0; y <= size.y; y++)
        {
            Color color = y % 10 == 0 ? GridMajorColor : (y % 2 == 0 ? GridColor : HalfGridColor);
            EditorGUI.DrawRect(new Rect(0f, y * halfPx, size.x * halfPx, 1f), color);
        }
    }

    /// <summary>画原点标记: 格子(0,0)的左上角就是房间节点的原点, 居中/对齐都以它为准</summary>
    private void DrawOriginMarker(Vector2Int origin)
    {
        float x = (0 - origin.x) * halfPx;
        float y = (0 - origin.y) * halfPx;
        Color color = new Color(0.25f, 0.6f, 1f, 1f);

        EditorGUI.DrawRect(new Rect(x - 1f, y - 8f, 2f, 16f), color);
        EditorGUI.DrawRect(new Rect(x - 8f, y - 1f, 16f, 2f), color);
    }

    private void DrawPieces(Vector2Int origin)
    {
        Dictionary<Vector2Int, int> occupied = CalcOccupied();

        for (int i = 0; i < target.pieces.Count; i++)
        {
            DungeonPiece piece = target.pieces[i];
            int col = ToHalf(piece.col);
            int row = ToHalf(piece.row);
            if (movingAll)
            {
                // 整体平移中: 所有块都跟着鼠标偏移画, 松手才写回数据
                col += moveDelta.x;
                row += moveDelta.y;
            }
            else if (i == movingPiece)
            {
                // 拖拽过程中先只画在鼠标位置, 松手才真正改数据
                col = movePreviewCell.x;
                row = movePreviewCell.y;
            }

            Rect rect = CellRect(origin, col, row, ToHalf(piece.cols), ToHalf(piece.rows));
            bool isStair = Test_DungeonLayout.IsStair(piece);
            EditorGUI.DrawRect(rect, isStair ? StairFill : (piece.isCorridor ? CorridorFill : RoomFill));

            bool highlighted = selectAll || i == selected || i == movingPiece;
            DrawRectOutline(rect, highlighted ? 3f : 1f, highlighted ? SelectBorder : PieceBorder);

            if (isStair)
            {
                GUI.Label(rect, StairArrow(piece.stair), pieceLabelStyle);
            }
            else if (piece.cols * cellPx > 34f && piece.rows * cellPx > 18f)
            {
                GUI.Label(rect, $"{FormatCells(piece.cols)}*{FormatCells(piece.rows)}", pieceLabelStyle);
            }

            DrawRecessMarks(piece, rect);
            DrawEdgeBlockMarks(piece, rect);
            DrawEdgePillarMarks(piece, rect);

            if (i != movingPiece && !movingAll)
            {
                DrawDoorMarks(piece, i, occupied, origin);
            }
        }
    }

    /// <summary>凹面/挖洞: 在房间矩形里按格比例画出来, 深色 = 这块地板被挖掉</summary>
    private void DrawRecessMarks(DungeonPiece piece, Rect pieceRect)
    {
        // 凹面的格号按房间工具的口径: 面的格数 = 向上取整(最后不足一整格的那半格也算一格)
        float cellW = pieceRect.width / Mathf.Max(1, Mathf.CeilToInt(piece.cols));
        float cellH = pieceRect.height / Mathf.Max(1, Mathf.CeilToInt(piece.rows));
        foreach (Test_RoomLayout.RoomRecess recess in piece.recesses)
        {
            Rect rect = new Rect(pieceRect.x + recess.col * cellW, pieceRect.y + recess.row * cellH,
                Mathf.Max(1, recess.cols) * cellW, Mathf.Max(1, recess.rows) * cellH);
            EditorGUI.DrawRect(rect, RecessFill);
            DrawRectOutline(rect, 2f, RecessBorder);
        }
    }

    /// <summary>边缘格围墙的标记: 在这一格的外边上画一条(墙体 = 粗白条, 栏杆 = 细蓝条, 空 = 红条)</summary>
    private void DrawEdgeBlockMarks(DungeonPiece piece, Rect pieceRect)
    {
        int cellCols = Mathf.CeilToInt(piece.cols);
        int cellRows = Mathf.CeilToInt(piece.rows);
        float cellW = pieceRect.width / Mathf.Max(1, cellCols);
        float cellH = pieceRect.height / Mathf.Max(1, cellRows);

        foreach (Test_RoomLayout.EdgeCellBlock block in piece.edgeBlocks)
        {
            bool alongX = block.face == Test_RoomLayout.RoomFace.ZMin || block.face == Test_RoomLayout.RoomFace.ZMax;
            Color color = block.kind == Test_RoomLayout.EdgeBlockKind.Wall ? WallMarkColor
                : (block.kind == Test_RoomLayout.EdgeBlockKind.Railing ? RailingMarkColor : EmptyMarkColor);
            float thickness = block.kind == Test_RoomLayout.EdgeBlockKind.Wall ? 5f : 3f;

            if (alongX)
            {
                // 上/下面: 格号 = 第几列(从 x 最小端数)
                int col = Mathf.Clamp(block.index, 0, cellCols - 1);
                float x = pieceRect.x + col * cellW;
                float width = Mathf.Max(1f, Mathf.Min(col + 1, cellCols) * cellW - col * cellW - 2f);
                float y = block.face == Test_RoomLayout.RoomFace.ZMin ? pieceRect.yMax - thickness : pieceRect.y;
                EditorGUI.DrawRect(new Rect(x + 1f, y, width, thickness), color);
            }
            else
            {
                // 左/右面: 格号从块最上边那一格数起(跟生成时同一套)
                int row = Mathf.Clamp(block.index, 0, cellRows - 1);
                float y = pieceRect.y + row * cellH;
                float height = Mathf.Max(1f, Mathf.Min(row + 1, cellRows) * cellH - row * cellH - 2f);
                float x = block.face == Test_RoomLayout.RoomFace.XMin ? pieceRect.x : pieceRect.xMax - thickness;
                EditorGUI.DrawRect(new Rect(x, y + 1f, thickness, height), color);
            }
        }
    }

    /// <summary>边缘格柱子的标记: 在这一格靠内那一侧画个小方块(柱子 = 浅灰, 开口柱子 = 黄, 空 = 红)</summary>
    private void DrawEdgePillarMarks(DungeonPiece piece, Rect pieceRect)
    {
        int cellCols = Mathf.CeilToInt(piece.cols);
        int cellRows = Mathf.CeilToInt(piece.rows);
        float cellW = pieceRect.width / Mathf.Max(1, cellCols);
        float cellH = pieceRect.height / Mathf.Max(1, cellRows);
        const float size = 7f;

        foreach (Test_RoomLayout.EdgeCellPillar edge in piece.edgePillars)
        {
            Color color = edge.kind == Test_RoomLayout.EdgePillarKind.Pillar ? PillarMarkColor
                : (edge.kind == Test_RoomLayout.EdgePillarKind.OpeningPillar ? DoorColor : EmptyMarkColor);
            bool alongX = edge.face == Test_RoomLayout.RoomFace.ZMin || edge.face == Test_RoomLayout.RoomFace.ZMax;
            float x;
            float y;
            if (alongX)
            {
                // 上/下面: 格号 = 第几列(从 x 最小端数)
                int col = Mathf.Clamp(edge.index, 0, cellCols - 1);
                x = pieceRect.x + (col + 0.5f) * cellW - size * 0.5f;
                y = edge.face == Test_RoomLayout.RoomFace.ZMin ? pieceRect.yMax - size - 3f : pieceRect.y + 3f;
            }
            else
            {
                // 左/右面: 格号从块最上边那一格数起(跟生成时同一套)
                int row = Mathf.Clamp(edge.index, 0, cellRows - 1);
                y = pieceRect.y + (row + 0.5f) * cellH - size * 0.5f;
                x = edge.face == Test_RoomLayout.RoomFace.XMin ? pieceRect.x + 3f : pieceRect.xMax - size - 3f;
            }

            EditorGUI.DrawRect(new Rect(x, y, size, size), color);
        }
    }

    /// <summary>楼梯的上坡箭头</summary>
    private static string StairArrow(StairDir dir)
    {
        switch (dir)
        {
            case StairDir.Up: return "↑";
            case StairDir.Down: return "↓";
            case StairDir.Left: return "←";
            case StairDir.Right: return "→";
        }

        return "";
    }

    /// <summary>画门标记: 直接跑一遍生成时的开口算法, 所以画出来的位置跟实际生成的门洞一致(半格也对)</summary>
    private void DrawDoorMarks(DungeonPiece piece, int index, Dictionary<Vector2Int, int> occupied, Vector2Int origin)
    {
        int cellRows = Mathf.CeilToInt(piece.rows);
        int hx0 = ToHalf(piece.col);
        int hz0 = ToHalf(piece.row);
        int halfCols = ToHalf(piece.cols);
        int halfRows = ToHalf(piece.rows);

        foreach (Test_RoomLayout.FaceOpening opening in Test_DungeonLayout.CalcOpenings(piece, index, occupied, Test_RoomLayout.OpeningPillarKind.Empty))
        {
            bool alongX = opening.face == Test_RoomLayout.RoomFace.ZMin || opening.face == Test_RoomLayout.RoomFace.ZMax;
            if (alongX)
            {
                // 沿z的面: 格号 = 第几列(从 x 最小端), 门画成横条
                float x = (hx0 + opening.index * 2 - origin.x) * halfPx + 2f;
                float width = Mathf.Min(2, halfCols - opening.index * 2) * halfPx - 4f;
                float y = opening.face == Test_RoomLayout.RoomFace.ZMin
                    ? (hz0 + halfRows - origin.y) * halfPx - 5f
                    : (hz0 - origin.y) * halfPx + 2f;
                EditorGUI.DrawRect(new Rect(x, y, width, 3f), DoorColor);
            }
            else
            {
                // 沿x的面: 格号从 z 最小端数(行号最大的那一端), 门画成竖条
                int r = cellRows - 1 - opening.index;
                float y = (hz0 + r * 2 - origin.y) * halfPx + 2f;
                float height = Mathf.Min(2, halfRows - r * 2) * halfPx - 4f;
                float x = opening.face == Test_RoomLayout.RoomFace.XMin
                    ? (hx0 - origin.x) * halfPx + 2f
                    : (hx0 + halfCols - origin.x) * halfPx - 5f;
                EditorGUI.DrawRect(new Rect(x, y, 3f, height), DoorColor);
            }
        }
    }

    private void DrawDraggingPiece(Vector2Int origin)
    {
        if (!dragging)
        {
            return;
        }

        Vector2Int min = Vector2Int.Min(dragStartCell, dragEndCell);
        Vector2Int max = Vector2Int.Max(dragStartCell, dragEndCell);
        Rect rect = CellRect(origin, min.x, min.y, max.x - min.x + 1, max.y - min.y + 1);
        EditorGUI.DrawRect(rect, DragFill);
        DrawRectOutline(rect, 1f, SelectBorder);
    }

    private void HandleCanvasEvent(Rect viewRect, Rect contentRect, Vector2Int origin, Vector2Int size)
    {
        Event current = Event.current;

        // 窗口坐标 -> 画布内容坐标(内容原点 = 滚动视图左上角 - 滚动值)
        Vector2 content = mouseWindowPos - viewRect.position + scroll;
        Vector2Int cell = ClampCell(ContentToHalf(content, origin), origin, size);

        // 松手事件即使鼠标滑到画布外面也要处理, 否则会卡在拖拽状态
        if (current.type == EventType.MouseUp && (movingPiece >= 0 || movingAll || dragging))
        {
            if (movingPiece >= 0 || movingAll)
            {
                CommitMove();
            }
            else
            {
                dragging = false;
                AddPiece(dragStartCell, dragEndCell);
            }

            current.Use();
            return;
        }

        // 鼠标要落在画布可视区域内, 并且落在棋盘上(避开滚动条)
        if (!viewRect.Contains(mouseWindowPos) || !contentRect.Contains(content))
        {
            return;
        }

        switch (current.type)
        {
            case EventType.MouseDown:
                // 只用左键: 画块 / 选块 / 拖块(右键不参与)
                if (current.button != 0)
                {
                    break;
                }

                if (tool == DrawTool.Select)
                {
                    int hit = PieceAt(cell);
                    if (hit >= 0)
                    {
                        // 按住左键直接拖也能搬动(全选状态下是整体平移; 只点不拖=收窄成选中它)
                        if (selectAll)
                        {
                            StartMoveAll(cell);
                        }
                        else
                        {
                            StartMove(hit, cell);
                        }
                    }
                    else
                    {
                        selected = -1;
                        selectAll = false;
                        Repaint();
                    }

                    current.Use();
                }
                else if (tool == DrawTool.Erase)
                {
                    int hit = PieceAt(cell);
                    if (hit >= 0)
                    {
                        RemovePiece(hit);
                    }

                    current.Use();
                }
                else
                {
                    int hit = PieceAt(cell);
                    if (hit >= 0)
                    {
                        // 这一格已经有房间/通道了: 不开始画, 改成选中它(继续按住拖就是搬动; 全选时是整体平移)
                        if (selectAll)
                        {
                            StartMoveAll(cell);
                        }
                        else
                        {
                            StartMove(hit, cell);
                        }
                    }
                    else
                    {
                        selectAll = false;   // 开始画新的块, 退出整体平移状态
                        dragging = true;
                        dragStartCell = cell;
                        dragEndCell = cell;
                    }

                    Repaint();
                    current.Use();
                }

                break;
            case EventType.MouseDrag:
                if (movingPiece >= 0 || movingAll)
                {
                    UpdateMove(cell);
                    current.Use();
                }
                else if (dragging)
                {
                    dragEndCell = cell;
                    Repaint();
                    current.Use();
                }

                break;
        }
    }

    /// <summary>左键按在已有块上时抓住它准备搬动, 先把预览位置放到它自己身上</summary>
    private void StartMove(int index, Vector2Int cell)
    {
        if (index < 0 || index >= target.pieces.Count)
        {
            return;
        }

        selected = index;
        movingPiece = index;
        moveGrabCell = cell;
        moveOriginCell = new Vector2Int(ToHalf(target.pieces[index].col), ToHalf(target.pieces[index].row));
        movePreviewCell = moveOriginCell;
        Repaint();
    }

    /// <summary>拖拽中: 只更新预览位置(松手才真正改数据并重新生成)</summary>
    private void UpdateMove(Vector2Int cell)
    {
        if (movingAll)
        {
            moveDelta = cell - moveGrabCell;
            Repaint();
            return;
        }

        if (movingPiece < 0)
        {
            return;
        }

        movePreviewCell = moveOriginCell + (cell - moveGrabCell);
        Repaint();
    }

    /// <summary>全选后拖拽: 记住所有块的原位置, 拖动只改预览偏移</summary>
    private void StartMoveAll(Vector2Int cell)
    {
        movingAll = true;
        movingPiece = -1;
        moveGrabCell = cell;
        moveDelta = Vector2Int.zero;
        moveOrigins.Clear();
        foreach (DungeonPiece piece in target.pieces)
        {
            moveOrigins.Add(new Vector2Int(ToHalf(piece.col), ToHalf(piece.row)));
        }

        Repaint();
    }

    /// <summary>整体平移: 没拖动就是"只想选一个", 拖动过才写回所有位置</summary>
    private void CommitMoveAll()
    {
        movingAll = false;
        if (moveDelta == Vector2Int.zero)
        {
            selectAll = false;
            selected = PieceAt(moveGrabCell);
            Repaint();
            return;
        }

        Undo.RecordObject(target, "整体平移房间/通道");
        for (int i = 0; i < target.pieces.Count && i < moveOrigins.Count; i++)
        {
            target.pieces[i].col = Cells(moveOrigins[i].x + moveDelta.x);
            target.pieces[i].row = Cells(moveOrigins[i].y + moveDelta.y);
        }

        Apply();
    }

    /// <summary>拖拽结束: 真正写回位置并重新生成</summary>
    private void CommitMove()
    {
        if (movingAll)
        {
            CommitMoveAll();
            return;
        }

        int index = movingPiece;
        DungeonPiece piece = target.pieces[index];
        movingPiece = -1;

        if (ToHalf(piece.col) == movePreviewCell.x && ToHalf(piece.row) == movePreviewCell.y)
        {
            Repaint();
            return;
        }

        Undo.RecordObject(target, "移动房间");
        piece.col = Cells(movePreviewCell.x);
        piece.row = Cells(movePreviewCell.y);
        selected = index;
        Apply();
    }

    /// <summary>把半格坐标夹进画布范围(origin/size 也是半格)</summary>
    private static Vector2Int ClampCell(Vector2Int cell, Vector2Int origin, Vector2Int size)
    {
        cell.x = Mathf.Clamp(cell.x, origin.x, origin.x + size.x - 1);
        cell.y = Mathf.Clamp(cell.y, origin.y, origin.y + size.y - 1);
        return cell;
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (selectAll)
        {
            EditorGUILayout.LabelField($"已全选 {target.pieces.Count} 个: 按住拖拽可整体平移; 点某个块=只选它; 按 Esc 取消选择。",
                EditorStyles.miniLabel);
        }
        else if (selected >= 0 && selected < target.pieces.Count)
        {
            DungeonPiece piece = target.pieces[selected];
            EditorGUI.BeginChangeCheck();

            // ===== 基础: 位置/尺寸/通道/墙面/楼梯方向 =====
            showPieceBasic = DrawModuleHeader(piece.isCorridor ? "选中:通道" : "选中:房间", 0, showPieceBasic, false, out _);
            if (showPieceBasic)
            {
                // 一行放完, 每项都按"1个字 + 3个数字"的紧凑宽度, 不铺满整行
                EditorGUILayout.BeginHorizontal();
                piece.col = CompactHalfField("列", piece.col);
                piece.row = CompactHalfField("行", piece.row);
                piece.cols = Mathf.Max(0.5f, CompactHalfField("宽", piece.cols));
                piece.rows = Mathf.Max(0.5f, CompactHalfField("高", piece.rows));
                piece.isCorridor = EditorGUILayout.ToggleLeft("通道", piece.isCorridor, GUILayout.Width(52f));

                if (GUILayout.Button("删除", GUILayout.Width(48f)))
                {
                    RemovePiece(selected);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.EndHorizontal();

                // 哪几面有墙: 下拉多选(没勾的面自动围栏杆; 和邻居贴边的面会再自动开口当门)
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("墙面", GUILayout.Width(34f));
                piece.wallFaces = (Test_RoomLayout.RoomFace)EditorGUILayout.EnumFlagsField(piece.wallFaces, GUILayout.Width(168f));
                EditorGUILayout.EndHorizontal();

                if (piece.isCorridor)
                {
                    // 通道可以改成楼梯: 上坡方向决定后面那些房间抬到多高
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("楼梯方向", GUILayout.Width(60f));
                    piece.stair = (StairDir)EditorGUILayout.EnumPopup(piece.stair, GUILayout.Width(80f));
                    EditorGUILayout.EndHorizontal();
                }
            }

            // ===== 边缘格围墙: 标题行(带条数)+ 加号, 明细折叠在里面 =====
            showEdgeBlocks = DrawModuleHeader("边缘格围墙", piece.edgeBlocks.Count, showEdgeBlocks, true, out bool addBlock);
            if (addBlock)
            {
                Undo.RecordObject(target, "加边缘格围墙");
                piece.edgeBlocks.Add(new Test_RoomLayout.EdgeCellBlock());
                showEdgeBlocks = true;
                Apply();
                Repaint();
            }

            if (showEdgeBlocks)
            {
                // 按 面 + 格号 挑一格, 单独换成 墙体/栏杆/空(每项一行)
                for (int i = 0; i < piece.edgeBlocks.Count; i++)
                {
                    Test_RoomLayout.EdgeCellBlock block = piece.edgeBlocks[i];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("围墙", GUILayout.Width(34f));
                    block.face = (Test_RoomLayout.RoomFace)EditorGUILayout.EnumPopup(block.face, GUILayout.Width(58f));
                    block.index = CompactIntField("格", block.index);
                    block.kind = (Test_RoomLayout.EdgeBlockKind)EditorGUILayout.EnumPopup(block.kind, GUILayout.Width(58f));
                    if (GUILayout.Button("×", GUILayout.Width(22f)))
                    {
                        Undo.RecordObject(target, "删除边缘格围墙");
                        piece.edgeBlocks.RemoveAt(i);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        Apply();
                        Repaint();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            // ===== 边缘格柱子: 标题行(带条数)+ 加号, 明细折叠在里面 =====
            showEdgePillars = DrawModuleHeader("边缘格柱子", piece.edgePillars.Count, showEdgePillars, true, out bool addPillar);
            if (addPillar)
            {
                Undo.RecordObject(target, "加边缘格柱子");
                piece.edgePillars.Add(new Test_RoomLayout.EdgeCellPillar());
                showEdgePillars = true;
                Apply();
                Repaint();
            }

            if (showEdgePillars)
            {
                // 按 面 + 格号 挑一格, 单独换成 柱子/开口柱子/空(每项一行)
                for (int i = 0; i < piece.edgePillars.Count; i++)
                {
                    Test_RoomLayout.EdgeCellPillar edge = piece.edgePillars[i];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("柱子", GUILayout.Width(34f));
                    edge.face = (Test_RoomLayout.RoomFace)EditorGUILayout.EnumPopup(edge.face, GUILayout.Width(58f));
                    edge.index = CompactIntField("格", edge.index);
                    edge.kind = (Test_RoomLayout.EdgePillarKind)EditorGUILayout.EnumPopup(edge.kind, GUILayout.Width(72f));
                    if (GUILayout.Button("×", GUILayout.Width(22f)))
                    {
                        Undo.RecordObject(target, "删除边缘格柱子");
                        piece.edgePillars.RemoveAt(i);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        Apply();
                        Repaint();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            // ===== 凹面/挖洞: 标题行(带条数)+ 加号, 明细折叠在里面 =====
            showRecesses = DrawModuleHeader("凹面/挖洞", piece.recesses.Count, showRecesses, true, out bool addRecess);
            if (addRecess)
            {
                Undo.RecordObject(target, "加凹面");
                piece.recesses.Add(new Test_RoomLayout.RoomRecess());
                showRecesses = true;
                Apply();
                Repaint();
            }

            if (showRecesses)
            {
                // 每项一行(列/行/宽/高): 贴房间边的按凹面围一圈, 不贴边的就只是地上的洞
                for (int i = 0; i < piece.recesses.Count; i++)
                {
                    Test_RoomLayout.RoomRecess recess = piece.recesses[i];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("凹面", GUILayout.Width(34f));
                    recess.col = CompactIntField("列", recess.col);
                    recess.row = CompactIntField("行", recess.row);
                    recess.cols = CompactIntField("宽", Mathf.Max(1, recess.cols));
                    recess.rows = CompactIntField("高", Mathf.Max(1, recess.rows));
                    if (GUILayout.Button("×", GUILayout.Width(22f)))
                    {
                        Undo.RecordObject(target, "删除凹面");
                        piece.recesses.RemoveAt(i);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        Apply();
                        Repaint();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Apply();
            }
        }
        else
        {
            EditorGUILayout.LabelField($"房间/通道 共 {target.pieces.Count} 个; 用「选择」点一个格子可以改尺寸/位置/墙面/楼梯方向/删除(尺寸可填 0.5 的倍数)。",
                EditorStyles.miniLabel);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(string.IsNullOrEmpty(target.lastResult) ? "还没生成" : target.lastResult, EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("清空布局", GUILayout.Width(80f)))
        {
            Undo.RecordObject(target, "清空布局");
            target.pieces.Clear();
            target.ClearAll();
            selected = -1;
            MarkDirty();
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    /// <summary>紧凑的数字输入: 一个字宽的标签 + 够3位数的输入框</summary>
    private static int CompactIntField(string label, int value)
    {
        GUILayout.Label(label, GUILayout.Width(18f));
        return EditorGUILayout.IntField(value, GUILayout.Width(42f));
    }

    /// <summary>
    /// 模块标题行: 折叠标题(有条数就带上)占一段固定宽度, "+" 紧跟在标题后面(不贴到最右).
    /// 返回折叠后的状态, addClicked 表示这一帧点了"+".
    /// </summary>
    private static bool DrawModuleHeader(string title, int count, bool expanded, bool showAdd, out bool addClicked)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginHorizontal(GUILayout.Width(HeaderTitleWidth));
        expanded = EditorGUILayout.Foldout(expanded, count > 0 ? $"{title} ({count})" : title, true);
        EditorGUILayout.EndHorizontal();

        addClicked = false;
        if (showAdd)
        {
            addClicked = GUILayout.Button("+", GUILayout.Width(22f));
        }

        EditorGUILayout.EndHorizontal();
        return expanded;
    }

    /// <summary>紧凑的格数输入(0.5 步进): 编辑完(回车/失焦)才写回, 免得打字打到一半就被吸附</summary>
    private static float CompactHalfField(string label, float value)
    {
        GUILayout.Label(label, GUILayout.Width(18f));
        return Test_DungeonLayout.SnapHalf(EditorGUILayout.DelayedFloatField(value, GUILayout.Width(42f)));
    }

    private void AddPiece(Vector2Int a, Vector2Int b)
    {
        Vector2Int min = Vector2Int.Min(a, b);
        Vector2Int max = Vector2Int.Max(a, b);
        int halfCols = max.x - min.x + 1;       // 拖了几个半格
        int halfRows = max.y - min.y + 1;

        DungeonPiece piece = new DungeonPiece
        {
            col = Cells(min.x),
            row = Cells(min.y),
            cols = Cells(halfCols),
            rows = Cells(halfRows),
            isCorridor = tool == DrawTool.Corridor,
            // 新块默认不贴墙(整圈栏杆), 要墙的面在下面「墙面」里单独勾
            wallFaces = Test_RoomLayout.RoomFace.None
        };

        if (piece.isCorridor)
        {
            // 通道压成 1 格宽, 沿拖拽更长的那一边
            if (halfCols >= halfRows)
            {
                piece.row = Cells(a.y);
                piece.rows = 1f;
            }
            else
            {
                piece.col = Cells(a.x);
                piece.cols = 1f;
            }
        }

        Undo.RecordObject(target, "画房间");
        target.pieces.Add(piece);
        selected = target.pieces.Count - 1;
        Apply();
    }

    private void RemovePiece(int index)
    {
        if (index < 0 || index >= target.pieces.Count)
        {
            return;
        }

        Undo.RecordObject(target, "删除房间");
        target.pieces.RemoveAt(index);
        selected = -1;
        Apply();
    }

    /// <summary>这一格(半格坐标)上是哪个块, 没有就 -1</summary>
    private int PieceAt(Vector2Int halfCell)
    {
        for (int i = target.pieces.Count - 1; i >= 0; i--)
        {
            DungeonPiece piece = target.pieces[i];
            if (halfCell.x >= ToHalf(piece.col) && halfCell.x < ToHalf(piece.col + piece.cols) &&
                halfCell.y >= ToHalf(piece.row) && halfCell.y < ToHalf(piece.row + piece.rows))
            {
                return i;
            }
        }

        return -1;
    }

    private Dictionary<Vector2Int, int> CalcOccupied()
    {
        return Test_DungeonLayout.BuildOccupied(target.pieces, out int _);
    }

    private Vector2Int CalcGridOrigin()
    {
        Vector2Int origin = Vector2Int.zero;
        foreach (DungeonPiece piece in target.pieces)
        {
            origin.x = Mathf.Min(origin.x, ToHalf(piece.col));
            origin.y = Mathf.Min(origin.y, ToHalf(piece.row));
        }

        origin.x -= 6;      // 留 3 格外边距(半格单位)
        origin.y -= 6;
        return origin;
    }

    private Vector2Int CalcGridSize(Vector2Int origin)
    {
        int cols = 60;      // 半格单位: 默认给 30 格
        int rows = 40;
        foreach (DungeonPiece piece in target.pieces)
        {
            cols = Mathf.Max(cols, ToHalf(piece.col + piece.cols) - origin.x + 6);
            rows = Mathf.Max(rows, ToHalf(piece.row + piece.rows) - origin.y + 6);
        }

        // 保证原点(0,0)那一格始终在画布里, 不然居中/对齐时看不到基准
        cols = Mathf.Max(cols, -origin.x + 8);
        rows = Mathf.Max(rows, -origin.y + 8);

        return new Vector2Int(cols + 1, rows + 1);
    }

    /// <summary>格子矩形: col/row/cols/rows 都是半格单位</summary>
    private Rect CellRect(Vector2Int origin, int col, int row, int cols, int rows)
    {
        return new Rect((col - origin.x) * halfPx, (row - origin.y) * halfPx, cols * halfPx, rows * halfPx);
    }

    /// <summary>画布坐标 -> 半格坐标</summary>
    private Vector2Int ContentToHalf(Vector2 content, Vector2Int origin)
    {
        return new Vector2Int(origin.x + Mathf.FloorToInt(content.x / halfPx), origin.y + Mathf.FloorToInt(content.y / halfPx));
    }

    private void DrawRectOutline(Rect rect, float thickness, Color color)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void Apply()
    {
        if (target == null)
        {
            return;
        }

        MarkDirty();
        if (autoGen)
        {
            target.GenAll();
            MarkDirty();
        }

        Repaint();
    }

    private void MarkDirty()
    {
        EditorUtility.SetDirty(target);
        if (target.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        }
    }

    /// <summary>
    /// 用当前选中的节点; 选中的对象上没挂布局组件时, 自动找一个布局节点
    /// (预制体隔离编辑时在正在编辑的预制体里找, 否则在当前场景里找)
    /// </summary>
    private void TryUseSelection(bool force)
    {
        if (!force && target != null)
        {
            return;
        }

        if (Selection.activeGameObject != null)
        {
            Test_DungeonLayout selected = Selection.activeGameObject.GetComponent<Test_DungeonLayout>();
            if (selected != null)
            {
                target = selected;
                Repaint();
                return;
            }
        }

        Test_DungeonLayout[] all = FindCandidateLayouts();
        if (all.Length == 0)
        {
            return;
        }

        target = all[0];
        if (all.Length > 1)
        {
            Debug.Log($"[房间拼装] 找到 {all.Length} 个布局节点, 先用了 {all[0].name}; 想换的话选中目标节点再点「用选中节点」");
        }

        Repaint();
    }

    /// <summary>
    /// 找候选布局节点: 预制体隔离编辑时只在正在编辑的预制体里找(FindObjectsByType 抓不到预制体 stage 的内容),
    /// 普通场景里则搜当前场景。
    /// </summary>
    private static Test_DungeonLayout[] FindCandidateLayouts()
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.prefabContentsRoot != null)
        {
            Test_DungeonLayout[] inPrefab = stage.prefabContentsRoot.GetComponentsInChildren<Test_DungeonLayout>(true);
            if (inPrefab.Length > 0)
            {
                return inPrefab;
            }
        }

        return UnityEngine.Object.FindObjectsByType<Test_DungeonLayout>(FindObjectsInactive.Include);
    }

    private void CreateLayoutNode()
    {
        Transform parent = Selection.activeTransform;
        if (parent == null)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            parent = stage != null ? stage.prefabContentsRoot.transform : null;
        }

        GameObject go = new GameObject("DungeonLayout");
        Undo.RegisterCreatedObjectUndo(go, "新建布局节点");
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        target = go.AddComponent<Test_DungeonLayout>();
        FillDefaultSetting();
        Selection.activeGameObject = go;
        MarkDirty();
        Repaint();
    }

    private void FillDefaultSetting()
    {
        if (target.prefabs == null)
        {
            target.prefabs = AssetDatabase.LoadAssetAtPath<RoomBlockSo>(DefaultSettingPath);
        }

        MarkDirty();
        Repaint();
    }
}
#endif
