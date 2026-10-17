using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao.Map
{
    /// <summary>
    /// 围墙/栏杆的"一格"数据: 每 ARepeatCount 段 A(墙段) 接 1 个连接点 B, 即 A A B.
    /// 尺寸都由子物体的 Mesh 包围盒算出来(按本节点本地坐标系的 X 轴), 供 Test_WallLayout 拼接用.
    /// 另外带拼接时的取整阈值(手填), 拼接工具会从这里读.
    /// </summary>
    public class Test_WallCell : MonoBehaviour
    {
        [Header("子物体顺序: A A ... B (先排 ARepeatCount 个 A, 再放 1 个 B)")]
        [MiniBtn(nameof(GetChild))] public List<GameObject> prefabs = new List<GameObject>();

        [Label("每几个A接一个B")] [Min(1)] public int ARepeatCount = 2;

        [ReadOnly] [Label("A的宽度(Mesh包围盒X)")] public float ABodyWidth;
        [ReadOnly] [Label("相邻两个A的重叠长度")] public float AACross;
        [ReadOnly] [Label("B的宽度(Mesh包围盒X)")] public float BWidth;
        [ReadOnly] [Label("A左边缘相对自己锚点")] public float ALeftOffset;

        [InfoBox("手填: 1 = 两个A之间刚好留出B的宽度; 0 = 两个A紧挨着(间距0), B压在中间")]
        [Min(0f)] [Label("B有效宽度(0~1)")] public float BValidWidth = 1f;

        /// <summary>段数取整阈值: 按距离反推出的段数小数部分超过它才进位多摆一段(重叠压紧一点凑距离), 否则舍掉小数(重叠削小把整条拉长)</summary>
        [Range(0f, 1f)] [Label("段数进位阈值")] public float lenFractionThreshold = 0.8f;

        /// <summary>实例化用的 A(子物体第 0 个)</summary>
        public GameObject ABodyPrefab => prefabs[0];

        /// <summary>实例化用的 B(子物体里 A 后面那一个)</summary>
        public GameObject BPrefab => prefabs[Mathf.Clamp(ARepeatCount, 0, Mathf.Max(0, prefabs.Count - 1))];

        private void GetChild()
        {
            prefabs = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                prefabs.Add(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 算尺寸: A的宽度、相邻两个A的重叠长度、B的宽度(都在本节点本地坐标系里量, 节点带旋转/缩放也不影响);
        /// 再算 A 的"左边缘相对自己锚点"的偏移 —— A 的锚点靠左, 拼接时按左边缘对齐; B 的锚点在中间, 直接按中心对齐.
        /// </summary>
        [Button("计算")]
        public void Caculate()
        {
            if (prefabs.Count < 2)
            {
                GetChild();
            }

            if (prefabs.Count < 2)
            {
                Debug.LogError("-- 格子里至少要有一个A和一个B(子物体顺序: A...A B)");
                return;
            }

            Bounds aBounds = GetLocalMeshBounds(prefabs[0], transform);
            ABodyWidth = aBounds.size.x;

            // 相邻两个A的实际间隔 = 两个中心差, 重叠 = A宽 - 间隔
            Bounds aNextBounds = GetLocalMeshBounds(prefabs[1], transform);
            AACross = ABodyWidth - (aNextBounds.center.x - aBounds.center.x);

            BWidth = GetLocalMeshBounds(BPrefab, transform).size.x;

            // A 的锚点靠左: 在"它自己的本地坐标系"里量包围盒左边缘(靠左锚点≈0)
            ALeftOffset = GetLocalMeshBounds(prefabs[0], prefabs[0].transform).min.x;
        }

        /// <summary>把子物体的Mesh包围盒转到 reference 的本地坐标系, 取包围盒(含旋转/缩放)</summary>
        private static Bounds GetLocalMeshBounds(GameObject target, Transform reference)
        {
            Matrix4x4 worldToLocal = reference.worldToLocalMatrix;
            bool hasBounds = false;
            Bounds result = new Bounds();

            foreach (MeshFilter meshFilter in target.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.sharedMesh == null)
                {
                    continue;
                }

                Matrix4x4 meshToLocal = worldToLocal * meshFilter.transform.localToWorldMatrix;
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;
                Vector3[] corners =
                {
                    new Vector3(min.x, min.y, min.z), new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z),
                };

                foreach (Vector3 corner in corners)
                {
                    Vector3 point = meshToLocal.MultiplyPoint3x4(corner);
                    if (!hasBounds)
                    {
                        result = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        result.Encapsulate(point);
                    }
                }
            }

            return result;
        }
    }
}
