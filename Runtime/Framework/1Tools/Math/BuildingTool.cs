using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 地编生成相关
    /// </summary>
    public static class BuildingTool
    {
        //包围盒生成
        public static void AddBoxCollider(Transform transform)
        {
            if (transform == null) return;

            // 在"节点本地坐标系"里算包围盒: 节点被抬高(比如楼梯上面的房间)或旋转后, 碰撞盒依然贴着模型
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            foreach (Renderer childRenderer in transform.GetComponentsInChildren<Renderer>())
            {
                if (!TryGetRendererLocalBounds(childRenderer, out Bounds rendererBounds))
                {
                    continue;
                }

                Matrix4x4 rendererToLocal = worldToLocal * childRenderer.transform.localToWorldMatrix;
                Vector3 min = rendererBounds.min;
                Vector3 max = rendererBounds.max;
                Vector3[] corners =
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z),
                };

                foreach (Vector3 corner in corners)
                {
                    Vector3 point = rendererToLocal.MultiplyPoint3x4(corner);
                    if (!hasBounds)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }

            if (hasBounds)
            {
                // 太矮的(栏杆之类)补成从脚下往上 5.6(世界高)的一堵"墙"; 按节点缩放折成本地高度, 免得跟着缩放变矮
                float minH = 8 * 0.7f / Mathf.Max(0.0001f, transform.lossyScale.y);
                if (bounds.size.y < minH)
                {
                    bounds.size = bounds.size.SetY(minH);
                    // 从节点原点(就是它被摆到地面/踏步上的那一点)往上长
                    bounds.center = bounds.center.SetY(minH * 0.5f);
                }

                // 获取或添加BoxCollider组件
                BoxCollider collider = transform.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = transform.gameObject.AddComponent<BoxCollider>();
                }

                // bounds 已经是本地坐标, 直接写进去
                collider.center = bounds.center;
                collider.size = bounds.size;
            }

            if (!LayerMask.LayerToName(transform.gameObject.layer).StartsWith("Wall"))
            {
                transform.gameObject.layer = Layers.WALL;
                Debug.Log($"-- set wall layer");
            }
        }

        private static bool TryGetRendererLocalBounds(Renderer renderer, out Bounds localBounds)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                localBounds = skinnedMeshRenderer.localBounds;
                return true;
            }

            if (renderer.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
            {
                localBounds = meshFilter.sharedMesh.bounds;
                return true;
            }

            localBounds = default;
            return false;
        }

        public static GameObject EditorInstancePrefab(GameObject prefab, Transform parent = null)
        {
#if UNITY_EDITOR
            if (!IsPrefab(prefab))
            {
                Debug.Log($"--- Not Prefab");
                return Object.Instantiate(prefab, parent);
            }
            else
            {
                Debug.Log($"--- IsPrefab");
                string path = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
                Object go = UnityEditor.PrefabUtility.InstantiatePrefab(
                    UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
                Debug.Log($"-- parent {parent}");
                return go as GameObject;
            }
#else
            return Object.Instantiate(prefab, parent);
#endif
        }

#if UNITY_EDITOR
        private static bool IsPrefab(GameObject pre)
        {
            bool isPrefab = false;

            isPrefab = UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(pre);

            if (!isPrefab && UnityEditor.PrefabUtility.IsPartOfPrefabAsset(pre))
            {
                isPrefab = true;
            }

            // isPrefab = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pre);
            //UnityEditor.PrefabUtility.IsPartOfPrefabAsset(pre);

            return isPrefab || UnityEditor.PrefabUtility.GetPrefabInstanceStatus(pre) ==
                UnityEditor.PrefabInstanceStatus.Connected;
        }
#endif

        // 封装成方法，返回Prefab实例化后所有子物体边界盒的包围盒  
        public static Bounds GetPrefabBounds(GameObject instantiatedPrefab)
        {
            // 用于存储所有子物体的边界盒  
            List<Bounds> boundsList = new List<Bounds>();

            // 遍历所有子物体（包括Prefab本身，如果它也是一个有效的边界盒来源）  
            void CollectBounds(GameObject go)
            {
                MeshFilter meshRenderer = go.GetComponent<MeshFilter>();
                if (meshRenderer != null && meshRenderer.sharedMesh != null)
                {
                    boundsList.Add(meshRenderer.sharedMesh.bounds);
                    //var bounds = boundsList[boundsList.Count - 1];
                    //bounds.center += go.transform.position; // 将边界盒中心调整到子物体的位置  
                }

                // 递归遍历所有子物体  
                foreach (Transform child in go.transform)
                {
                    CollectBounds(child.gameObject);
                }
            }

            CollectBounds(instantiatedPrefab);

            // 如果没有找到任何MeshRenderer，则返回默认边界盒  
            if (boundsList.Count == 0)
            {
                //Destroy(instantiatedPrefab);
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            // 计算边界盒的并集  
            Bounds enclosingBounds = boundsList[0];
            for (int i = 1; i < boundsList.Count; i++)
            {
                enclosingBounds.Encapsulate(boundsList[i]);
            }

            // 销毁实例化的Prefab  
            //Destroy(instantiatedPrefab);

            return enclosingBounds;
        }
    }
}
