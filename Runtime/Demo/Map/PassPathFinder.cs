using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>敌人的移动模式</summary>
    public enum EnemyMoveMode
    {
        [InspectorName("默认模式")] Default,
        [InspectorName("寻路模式")] Path,
    }

    /// <summary>寻路查询结果</summary>
    public enum PassPathResult
    {
        /// <summary>直线可达, 直接朝目标走</summary>
        Direct,

        /// <summary>被挡住但能绕: 沿通道连线点走, 路径已写入 outPath</summary>
        Follow,

        /// <summary>不可达: 距离过远且视线被墙挡住, 或地图上没有可用的通道连线</summary>
        Unreachable,
    }

    /// <summary>
    /// 地图通道寻路: 用 <see cref="PassPointCollector"/> 收集的通道端点建一张无向图,
    /// 在图上做 A* 得到一条绕开 Wall / WallBlock 的路线。
    /// <para>连通关系全部来自地图生成时记录的数据, 通道点之间不做射线检测:</para>
    /// <para>1. 同一条 PassLine 上的相邻点必定连通(通道本身);</para>
    /// <para>2. 同一个 <see cref="PassRoom"/> 分组里的点必定连通(房间内部没有墙);</para>
    /// <para>3. 接入时半径内的点都算出入口, 并且距离最近的那个必定可达。</para>
    /// <para>视线检测只保留在三处: 角色与目标的"直接看得见就走直线", 角色与出入口之间(选了走不到的门洞会让敌人贴着墙停住),
    /// 以及路径拉直。</para>
    /// <para>图在第一次查询时构建并缓存, 换地图时由 <see cref="Invalidate"/> 或收集器被销毁触发重建。</para>
    /// </summary>
    public static class PassPathFinder
    {
        /// <summary>打视线时从脚下抬高这么多, 避免贴地打线被地面挡住</summary>
        private const float EyeHeight = 1.2f;

        /// <summary>老地图兜底连边的距离上限(没有房间分组数据时才用)</summary>
        private const float MaxLinkDistance = 30f;

        /// <summary>挡视线的层: 墙 + 墙体碰撞盒</summary>
        private static int BlockMask => Layers.WALL_MASK | Layers.WALL_BLOCK_MASK;

        private static PassPointCollector _collector;

        private static readonly List<Vector3> _nodes = new List<Vector3>();

        /// <summary>邻接表: 下标和 _nodes 一一对应</summary>
        private static readonly List<List<int>> _links = new List<List<int>>();

        /// <summary>通道端点节点 -> 节点下标, 用来把房间分组登记的端点接上</summary>
        private static readonly Dictionary<Transform, int> _nodeOf = new Dictionary<Transform, int>();

        /// <summary>是否已经尝试建过图(没有收集器时也置位, 避免每次查询都全场景搜一遍)</summary>
        private static bool _built;

        private static bool _warnedNoPoint;

        private static bool _warnedNoRoom;

        /// <summary>
        /// 地图换了之后清掉缓存, 下一次查询重新建图。
        /// </summary>
        public static void Invalidate()
        {
            _built = false;
            _collector = null;
            _nodes.Clear();
            _links.Clear();
            _nodeOf.Clear();
        }

        #region 查询

        /// <summary>
        /// 查询 from 到 to 的路线。
        /// <para>看得见 = 直接走; 看不见就在通道图上找绕路点, 只有"接不上图"或"图里走不通"才算不可达。</para>
        /// <para>接入通道点只看距离, 不做遮挡检测: 半径内的点都算出入口, 另外距离最近的那一个必定算可达。</para>
        /// <para>farDistance 只是接入半径, 不是"太远就放弃"的门槛:
        /// 玩家在楼上、敌人在楼下时直线距离本来就会超过它(实测 27 米),
        /// 一旦拿它当门槛卡住, 敌人会每帧判定不可达而原地站死。</para>
        /// outPath 是路径点(世界坐标), 最后一个是 to; Direct 时只有一个点。
        /// </summary>
        public static PassPathResult Query(Vector3 from, Vector3 to, float farDistance, List<Vector3> outPath)
        {
            outPath.Clear();

            if (HasLineOfSight(from, to))
            {
                outPath.Add(to);
                return PassPathResult.Direct;
            }

            EnsureGraph();
            if (_nodes.Count < 2)
            {
                return PassPathResult.Unreachable;
            }

            // 出入口: 半径内的通道点都算, 另外距离最近的那一个必定算可达。
            // 但"角色 → 通道点"这一段是真要走过去的: 直线被墙挡住的点走不到,
            // 选它当出口会让路径最后一段变成"从那个点直穿墙"(实测敌人会顶在墙前静止好几秒)。
            // 所以两端都优先只留直线走得到的点; 一个都走不到时再退回最近点, 保证不会因此判不可达。
            List<int> starts = VisibleNodes(from, farDistance);
            KeepReachable(starts, from);
            if (starts.Count == 0)
            {
                AddNearestNode(starts, from);
            }

            List<int> goals = VisibleNodes(to, farDistance);
            KeepReachable(goals, to);
            if (goals.Count == 0)
            {
                AddNearestNode(goals, to);
            }

            if (starts.Count == 0 || goals.Count == 0)
            {
                return PassPathResult.Unreachable;
            }

            List<int> nodePath = AStar(starts, goals, from, to);
            if (nodePath == null)
            {
                return PassPathResult.Unreachable;
            }

            foreach (int id in nodePath)
            {
                outPath.Add(_nodes[id]);
            }

            outPath.Add(to);
            Simplify(outPath);
            return PassPathResult.Follow;
        }

        /// <summary>两点之间有没有被 Wall / WallBlock 挡住</summary>
        public static bool HasLineOfSight(Vector3 a, Vector3 b)
        {
            Vector3 from = a + Vector3.up * EyeHeight;
            Vector3 to = b + Vector3.up * EyeHeight;
            return !Physics.Linecast(from, to, BlockMask, QueryTriggerInteraction.Ignore);
        }

        #endregion

        #region 建图

        private static void EnsureGraph()
        {
            if (_built)
            {
                return;
            }

            BuildGraph();
        }

        private static void BuildGraph()
        {
            _built = true;
            _collector = FindCollector();
            _nodes.Clear();
            _links.Clear();
            _nodeOf.Clear();

            if (_collector == null || _collector.lines == null)
            {
                if (!_warnedNoPoint)
                {
                    _warnedNoPoint = true;
                    Debug.LogWarning("[PassPathFinder] 地图里没有 PassPointCollector, 寻路模式无法绕路; " +
                        "用 Test_DungeonLayout 生成地图后保存预制体即可");
                }

                return;
            }

            _warnedNoPoint = false;

            // 1. 同一条连线上的相邻点是通道本身, 直接连通(通道里能走, 不看遮挡)
            foreach (PassLine line in _collector.lines)
            {
                if (line == null || line.points == null)
                {
                    continue;
                }

                List<int> ids = new List<int>();
                foreach (Transform point in line.points)
                {
                    if (point == null)
                    {
                        continue;
                    }

                    _nodes.Add(point.position);
                    _links.Add(new List<int>());
                    ids.Add(_nodes.Count - 1);
                    _nodeOf[point] = _nodes.Count - 1;
                }

                for (int i = 1; i < ids.Count; i++)
                {
                    Link(ids[i - 1], ids[i]);
                }
            }

            // 2. 同一个房间里的通道端点互通: 房间内部没有墙, 分组由地图生成时按"端面贴着哪个房间"记好,
            //    这里只按分组连边, 不做射线检测
            if (!LinkRooms())
            {
                // 3. 老地图(生成于加房间分组之前)没有这份数据: 退回按视线连边, 并提示重新生成一次
                LinkByLineOfSight();
                if (!_warnedNoRoom)
                {
                    _warnedNoRoom = true;
                    Debug.LogWarning("[PassPathFinder] 地图里没有房间分组数据, 暂时按视线判定通道点连通; " +
                        "用 Test_DungeonLayout 重新生成一次地图就会改成按房间分组(完全不依赖射线)");
                }
            }
        }

        /// <summary>
        /// 按房间分组连边: 同一房间里的端点两两连通。返回 false 表示地图上没有这份数据。
        /// </summary>
        private static bool LinkRooms()
        {
            if (_collector.rooms == null || _collector.rooms.Count == 0)
            {
                return false;
            }

            bool linked = false;
            foreach (PassRoom room in _collector.rooms)
            {
                if (room == null || room.points == null)
                {
                    continue;
                }

                List<int> ids = new List<int>();
                foreach (Transform point in room.points)
                {
                    int id;
                    if (point != null && _nodeOf.TryGetValue(point, out id))
                    {
                        ids.Add(id);
                    }
                }

                for (int i = 0; i < ids.Count; i++)
                {
                    for (int j = i + 1; j < ids.Count; j++)
                    {
                        Link(ids[i], ids[j]);
                        linked = true;
                    }
                }
            }

            return linked;
        }

        /// <summary>老地图的兜底: 互相看得见的两点也连通(同一个房间里的门口), 靠距离上限控制边数</summary>
        private static void LinkByLineOfSight()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                for (int j = i + 1; j < _nodes.Count; j++)
                {
                    if (HasLink(i, j))
                    {
                        continue;
                    }

                    if ((_nodes[i] - _nodes[j]).sqrMagnitude > MaxLinkDistance * MaxLinkDistance)
                    {
                        continue;
                    }

                    if (!HasLineOfSight(_nodes[i], _nodes[j]))
                    {
                        continue;
                    }

                    Link(i, j);
                }
            }
        }

        private static PassPointCollector FindCollector()
        {
            // 只找激活的: MapMgr 会把没在用的关卡整个藏起来
            PassPointCollector[] all =
                Object.FindObjectsByType<PassPointCollector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return all.Length > 0 ? all[0] : null;
        }

        private static void Link(int a, int b)
        {
            if (!_links[a].Contains(b))
            {
                _links[a].Add(b);
            }

            if (!_links[b].Contains(a))
            {
                _links[b].Add(a);
            }
        }

        private static bool HasLink(int a, int b)
        {
            return _links[a].Contains(b);
        }

        #endregion

        #region A*

        /// <summary>从任意起点通道点走到任意终点通道点的最短路径(节点下标序列), 走不通返回 null</summary>
        private static List<int> AStar(List<int> starts, List<int> goals, Vector3 from, Vector3 to)
        {
            int count = _nodes.Count;
            float[] g = new float[count];
            int[] prev = new int[count];
            bool[] closed = new bool[count];
            for (int i = 0; i < count; i++)
            {
                g[i] = float.MaxValue;
                prev[i] = -1;
            }

            List<int> open = new List<int>();
            foreach (int id in starts)
            {
                float cost = Vector3.Distance(from, _nodes[id]);
                if (cost < g[id])
                {
                    g[id] = cost;
                    open.Add(id);
                }
            }

            while (open.Count > 0)
            {
                int bestIndex = 0;
                float bestF = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    int id = open[i];
                    float f = g[id] + Heuristic(id, goals);
                    if (f < bestF)
                    {
                        bestF = f;
                        bestIndex = i;
                    }
                }

                int cur = open[bestIndex];
                open.RemoveAt(bestIndex);

                if (goals.Contains(cur))
                {
                    List<int> path = new List<int>();
                    for (int id = cur; id >= 0; id = prev[id])
                    {
                        path.Add(id);
                    }

                    path.Reverse();
                    return path;
                }

                if (closed[cur])
                {
                    continue;
                }

                closed[cur] = true;

                foreach (int next in _links[cur])
                {
                    if (closed[next])
                    {
                        continue;
                    }

                    float nextG = g[cur] + Vector3.Distance(_nodes[cur], _nodes[next]);
                    if (nextG >= g[next])
                    {
                        continue;
                    }

                    g[next] = nextG;
                    prev[next] = cur;
                    open.Add(next);
                }
            }

            return null;
        }

        /// <summary>到最近终点通道点的直线距离, 作为 A* 的启发值</summary>
        private static float Heuristic(int id, List<int> goals)
        {
            float best = float.MaxValue;
            foreach (int goal in goals)
            {
                float distance = Vector3.Distance(_nodes[id], _nodes[goal]);
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        #endregion

        /// <summary>
        /// 从某个位置能接入的通道点(当作出入口)。
        /// <para>只按距离判定: 通道点在 farDistance 以内就算可接入, 不做遮挡判定 ——
        /// 人站在房间里、门洞不在正前方时也能接上图, 否则会每帧判定不可达而原地卡死。</para>
        /// </summary>
        private static List<int> VisibleNodes(Vector3 pos, float farDistance)
        {
            List<int> result = new List<int>();
            float sqrLimit = farDistance * farDistance;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if ((_nodes[i] - pos).sqrMagnitude <= sqrLimit)
                {
                    result.Add(i);
                }
            }

            return result;
        }

        /// <summary>
        /// 只保留从 pos 直线走得到的通道点(中间没有 Wall / WallBlock 挡着)。
        /// <para>一个都走不到时, 保留最近的那一个: 宁可让它先朝门口走, 也不能在这里判成不可达
        /// (出生点离门洞比半径还远的情况很常见, 直接判不可达会让敌人原地站死)。</para>
        /// </summary>
        private static void KeepReachable(List<int> ids, Vector3 pos)
        {
            if (ids.Count <= 1)
            {
                return;
            }

            int nearest = -1;
            float nearestSqr = float.MaxValue;
            List<int> reachable = new List<int>(ids.Count);
            foreach (int id in ids)
            {
                float sqr = (_nodes[id] - pos).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = id;
                }

                if (HasLineOfSight(pos, _nodes[id]))
                {
                    reachable.Add(id);
                }
            }

            if (reachable.Count == 0)
            {
                if (nearest >= 0)
                {
                    reachable.Add(nearest);
                }
            }

            ids.Clear();
            ids.AddRange(reachable);
        }

        /// <summary>取离 pos 最近的通道点当出入口(已经在列表里就不重复加)</summary>
        private static void AddNearestNode(List<int> result, Vector3 pos)
        {
            int best = -1;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _nodes.Count; i++)
            {
                float sqr = (_nodes[i] - pos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }

            if (best >= 0 && !result.Contains(best))
            {
                result.Add(best);
            }
        }

        /// <summary>拉直路径: 中间点能跳过就跳过(相邻两点互相可见), 避免贴着每个门口拐直角</summary>
        private static void Simplify(List<Vector3> points)
        {
            int i = 0;
            while (i + 2 < points.Count)
            {
                if (HasLineOfSight(points[i], points[i + 2]))
                {
                    points.RemoveAt(i + 1);
                }
                else
                {
                    i++;
                }
            }
        }
    }
}
