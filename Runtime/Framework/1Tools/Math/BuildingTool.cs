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

            // 获取所有子物体的渲染器边界
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;

            foreach (Transform child in transform)
            {
                Renderer childRenderer = child.GetComponent<Renderer>();
                if (childRenderer != null)
                {
                    if (!hasBounds)
                    {
                        bounds = childRenderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(childRenderer.bounds);
                    }
                }
            }

            if (hasBounds)
            {
                float minH = 8 * 0.7f;
                if (bounds.size.y < minH)
                {
                    bounds.size = bounds.size.SetY(minH);
                    bounds.center = bounds.center.SetY(minH / 2f);
                }

                // 获取或添加BoxCollider组件
                BoxCollider collider = transform.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = transform.gameObject.AddComponent<BoxCollider>();
                }

                // 设置包围盒参数
                collider.center = transform.InverseTransformPoint(bounds.center);
                collider.size = transform.InverseTransformVector(bounds.size);
            }

            if (!LayerMask.LayerToName(transform.gameObject.layer).StartsWith("Wall"))
            {
                transform.gameObject.layer = Layers.WALL;
                Debug.Log($"-- set wall layer");
            }
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