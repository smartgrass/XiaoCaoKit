using NaughtyAttributes;
using UnityEngine;
/// <summary>
/// 绘制 扇形，环形，扇环
/// </summary>
///源: https://blog.csdn.net/qq_26444231/article/details/131437542
//[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class DrawSector : MonoBehaviour
{
    //public bool isDrawing = true;

    public float OuterRadius = 6; //外半径  
    public float InnerRadius = 3; //外半径  
    public float Height = 3; //高度  
    public float angleDegree = 360; //扇形或扇面的角度
    public int Segments = 20; //分割数  
    private MeshFilter meshFilter;

    public bool DrawGizmos;

    public class BoundingShape
    {
        public float Angle;
        public float Height;
        public float Radius;
        public float Length;
    }

    void OnEnable()
    {
        UpdateShape();
    }

    public void SetShape(BoundingShape shape)
    {
        angleDegree = shape.Angle;
        Height = shape.Height;
        OuterRadius = shape.Radius;
        InnerRadius = shape.Length;
        UpdateShape();
    }

    private void Update()
    {
        UpdateShape();
    }

    [Button]
    void UpdateShape()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = MathLayoutTool.GetSectorMesh(angleDegree, OuterRadius, Height, Segments);
    }


    void OnDrawGizmosSelected()
    {
        if (!DrawGizmos)
        {
            return;
        }
        Matrix4x4 originalMatrix = Gizmos.matrix; //保存原来矩阵
        Gizmos.matrix = transform.localToWorldMatrix;
        meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;
        if (mesh != null)
        {
            var vertices = mesh.vertices;
            for (int i = 0; i < mesh.vertices.Length - 2; i++)
            {
                //
                Vector3 center = CalculateCenterPoint(vertices[i], vertices[i + 1], vertices[i + 2]);
                Gizmos.DrawRay(center, mesh.normals[i]);
            }
        }
        
        Gizmos.matrix = originalMatrix; //绘制结束后修改回来
    }


    Vector3 CalculateCenterPoint(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // 计算每个坐标的平均值  
        float centerX = (p1.x + p2.x + p3.x) / 3.0f;

        float centerY = (p1.y + p2.y + p3.y) / 3.0f;

        float centerZ = (p1.z + p2.z + p3.z) / 3.0f;
        // 返回中心点  
        return new Vector3(centerX, centerY, centerZ);

    }
}