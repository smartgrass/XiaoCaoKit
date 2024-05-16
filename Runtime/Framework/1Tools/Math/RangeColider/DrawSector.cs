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
        meshFilter.mesh = MathLayoutTool.GetSectorMesh(angleDegree, OuterRadius, Height, Segments, InnerRadius);
    }
}