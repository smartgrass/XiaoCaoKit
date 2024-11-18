
#if UNITY_EDITOR
using NaughtyAttributes;
using System.Collections.Generic;
using XiaoCao;
using UnityEditor;
using UnityEngine;
using static MathLayoutTool;
using System.Drawing;

public class Test_MapLayout : MonoBehaviour
{
    [MiniBtn(nameof(GetBounds))]
    [OnValueChanged(nameof(GetBounds))]
    public GameObject prefab;

    public Vector3Int genXYZ = Vector3Int.one;
    [Label("追加间距")]
    public Vector3 addValue;

    [Label("是否空心/墙体")]
    public bool IsHollow = true;

    //边缘分组
    public bool sortGroup = true;


    public string prefabName;
    [ReadOnly]
    public Vector3 boxSize;
    [ReadOnly]
    public Vector3 center;




    void GetBounds()
    {
        if (prefab == null)
        {
            return;
        }
        Bounds b = PrefabUtils.GetPrefabBounds(prefab);
        boxSize = b.size;
        center = b.center;
    }


    [Button("生成/排列")]
    void TestGent()
    {
        if (prefab == null)
        {
            return;
        }
        GridArrangementTool grid = new GridArrangementTool(genXYZ.x, genXYZ.z, genXYZ.y, IsHollow);
        Debug.Log($"--- grid {grid.TotalCells()}");

        Vector3 size = boxSize + addValue;
        int count = grid.TotalCells();
        for (int n = 0; n < count; n++)
        {
            var obj = GetObject(n);
            obj.name = $"{prefabName}{n}";
            var point = grid.GetCoordinates(n);

            var x = (point.Item1) * size.x; //+ center.x/2;
            var y = (point.Item2) * size.y; //+ center.y/2;
            var z = (point.Item3) * size.z;// + center.z/2;
            obj.transform.localPosition = new Vector3(x, y, z);
        }

        int delta = transform.childCount - count;
        if (delta > 0)
        {
            for (int i = 0; i < delta; i++)
            {
                var go = transform.GetChild(count).gameObject;
                GameObject.DestroyImmediate(go);
            }
        }
    }

    [Button("选择边缘X", -10)]
    void SelectBorderX()
    {
        int prefabIndex = 0;
        for (int i = 0; i < genXYZ.x; i++)
        {
            bool isBorder_I = i == 0 || i == genXYZ.x - 1;
            for (int j = 0; j < genXYZ.y; j++)
            {
                bool isBorder_J = j == 0 || j == genXYZ.y - 1;
                for (int k = 0; k < genXYZ.z; k++)
                {
                    bool isBorder_K = k == 0 || k == genXYZ.z - 1;

                    if (IsHollow && !isBorder_I && !isBorder_K)
                    {
                        continue;
                    }
                }
                
            }
        }
    }

    [Button("选择边缘Z", -10)]
    void SelectBorderZ()
    {

    }


    private int GetIndex(int x, int y, int z)
    {
        //y层,x列 ,z行
        return y * (int)genXYZ.x * (int)genXYZ.z + z * (int)genXYZ.x + x;
    }

    public GameObject GetObject(int i)
    {
        if (transform.childCount > i)
        {
            return transform.GetChild(i).gameObject;
        }
        else
        {
            if (IsPrefab())
            {
                Debug.Log($"--- IsPrefab");
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
                Object go = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), transform);
                return go as GameObject;
            }
            else
            {
                return GameObject.Instantiate(prefab, transform);
            }
        }
    }

    private bool IsPrefab()
    {
        bool isPrefab = false;

        isPrefab = PrefabUtility.IsPartOfPrefabAsset(prefab);

        return isPrefab || PrefabUtility.GetPrefabInstanceStatus(prefab) == PrefabInstanceStatus.Connected;
    }

}

public class PrefabUtils
{
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



public class RoomGenerator 
{
    public GameObject wallPrefab; // Wall预制体  
    public float roomWidth = 10f; // 房间的宽度  
    public float roomDepth = 10f; // 房间的深度  
    public float wallThickness = 1f; // 墙体的厚度  
    public float hollowWidth = 4f; // 空心部分的宽度  


    void GenerateRoom()
    {
        //// 计算墙体位置  
        //float halfWidth = roomWidth / 2f;
        //float halfDepth = roomDepth / 2f;
        //float halfThickness = wallThickness / 2f;
        //float hollowHalfWidth = hollowWidth / 2f;

        //// 生成外部墙体  
        //// 前  
        //GameObject.Instantiate(wallPrefab, new Vector3(halfWidth - halfThickness, 0, halfDepth - halfThickness), Quaternion.identity, transform);
        //// 后  
        //GameObject.Instantiate(wallPrefab, new Vector3(halfWidth - halfThickness, 0, -halfDepth + halfThickness), Quaternion.identity, transform);
        //// 左  
        //GameObject.Instantiate(wallPrefab, new Vector3(-halfWidth + halfThickness, 0, 0), Quaternion.Euler(0, 90, 0), transform);
        //// 右  
        //GameObject.Instantiate(wallPrefab, new Vector3(halfWidth - hollowHalfWidth - halfThickness, 0, 0), Quaternion.Euler(0, 90, 0), transform);

        //// 如果需要，你可以添加更多墙体来封闭顶部和底部  

        //// 生成空心部分两侧的墙体  
        //// 左侧空心墙  
        //GameObject.Instantiate(wallPrefab, new Vector3(-hollowHalfWidth + halfThickness, 0, -halfDepth + halfThickness), Quaternion.Euler(0, 90, 0), transform);
        //GameObject.Instantiate(wallPrefab, new Vector3(-hollowHalfWidth + halfThickness, 0, halfDepth - halfThickness), Quaternion.Euler(0, 90, 0), transform);

        // 右侧空心墙（可选，取决于设计）  
        // 类似地实例化，但调整x坐标  
    }
}

#else

#endif
