using Flux;
using System;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;
using Color = UnityEngine.Color;

public class RayCasterTrigger : MonoBehaviour, ITrigger
{
    public MeshInfo meshInfo;

    public int layerMask;

    public Action<Collider> TriggerAct { get; set; }

    public MeshType MeshType => meshInfo.meshType;

    RaycastHit[] hits = new RaycastHit[16];

    public HashSet<Collider> tempColliders = new HashSet<Collider>();

    private float distance = 0;

    Vector3 WorldCenter { get { return transform.InverseTransformPoint(meshInfo.GetCenter); } }

    Vector3 WorldEulerAngles { get { return transform.eulerAngles + meshInfo.GetEulerAngles; } }

    public void InitListener(Action<Collider> action)
    {
        TriggerAct = null;
        TriggerAct += action;
        tempColliders.Clear();
    }

    public void Switch(bool isOn)
    {
        enabled = isOn;
        tempColliders.Clear();
    }

    private void FixedUpdate()
    {
        switch (MeshType)
        {
            case MeshType.Box:
                OnBox();
                break;
            case MeshType.Sphere:
                OnSphere();
                break;
            case MeshType.Sector:
                OnSector();
                break;
            case MeshType.Other:
                break;
            default:
                break;
        }
    }



    private void DoTrigger(Collider collider)
    {
        if (tempColliders.Contains(collider))
        {
            TriggerAct?.Invoke(collider);
            tempColliders.Add(collider);
        }
    }

    private void ClearTemp()
    {
        tempColliders.Clear();
    }

    private void OnBox()
    {
        int hitCount = Physics.BoxCastNonAlloc(WorldCenter, meshInfo.GetSize / 2, WorldEulerAngles, hits, Quaternion.Euler(WorldEulerAngles), distance, layerMask);
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                DoTrigger(hits[i].collider);
            }
        }
    }

    private void OnSphere()
    {
        int hitCount = Physics.SphereCastNonAlloc(WorldCenter, meshInfo.GetRadius, WorldEulerAngles, hits, distance, layerMask);
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                DoTrigger(hits[i].collider);
            }
        }
    }

    private void OnSector()
    {
        int hitCount = Physics.SphereCastNonAlloc(WorldCenter, meshInfo.GetRadius, WorldEulerAngles, hits, distance, layerMask);
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Vector3 hitPoint = hits[i].point;
                float hight = meshInfo.GetHight;
                float angleRange = meshInfo.GetRadian;

                //算出点在扇形柱空间的局部坐标
                Quaternion rotationQuaternion = Quaternion.Euler(WorldEulerAngles);
                Vector3 localPoint = Quaternion.Inverse(rotationQuaternion) * (hitPoint - WorldCenter);

                if (IsPointWithinSectorCylinder(localPoint, hight, meshInfo.GetRadius, angleRange))
                {
                    DoTrigger(hits[i].collider);
                }
            }
        }

#if UNITY_EDITOR
        //ForTest

#endif
    }


    static bool IsPointWithinSectorCylinder(Vector3 localPoint, float h, float r, float a)
    {
        // 在这里，我们假设局部坐标系的原点就是扇形的圆心，y轴（或up方向）是圆柱体的高度方向
        float heightCheck = Mathf.Abs(localPoint.y); // 在局部坐标系中，y轴代表高度
        bool isWithinCylinder = (heightCheck <= h / 2) && (Mathf.Sqrt(localPoint.x * localPoint.x + localPoint.z * localPoint.z) <= r);

        if (!isWithinCylinder)
            return false;


        // 计算点相对于圆心的角度（在xz平面上，从x轴正方向逆时针测量）
        float angleToPoint = Mathf.Atan2(localPoint.z, localPoint.x);

        // 将角度标准化到 [0, 2*PI)
        if (angleToPoint < 0)
        {
            angleToPoint += 2 * Mathf.PI;
        }
        // 假设扇形从x轴正方向开始，逆时针测量角度范围
        float startAngle = -a / 2; // 可以根据需要修改这个值
        float endAngle = startAngle + a;
        if (endAngle > 2 * Mathf.PI)
        {
            endAngle -= 2 * Mathf.PI;
        }
        // 检查点是否在扇形角度范围内
        bool isWithinSector = (angleToPoint >= startAngle && angleToPoint <= endAngle) ||

                              (endAngle < startAngle && (angleToPoint >= startAngle || angleToPoint <= endAngle));
        return isWithinSector;

    }



    public float height = 10.0f;
    public float radius = 5.0f;
    public float angle = 45.0f; // 扇形的角度范围（度数）
    public Vector3 center = Vector3.zero; // 扇形的中心点（在世界坐标系中）
    public Quaternion rotation = Quaternion.identity; // 扇形的旋转（在世界坐标系中）

    void OnDrawGizmos()
    {
        // 设置Gizmos的颜色和大小
        Gizmos.color = UnityEngine.Color.red;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, rotation, Vector3.one); // 应用扇形柱的世界位置和旋转

        // 绘制圆柱体的侧面
        int numSegments = 36; // 用于绘制圆弧的线段数量
        float segmentAngle = angle * Mathf.Deg2Rad / numSegments; // 每段圆弧的角度

        Vector3[] topVertices = new Vector3[numSegments + 1];
        Vector3[] bottomVertices = new Vector3[numSegments + 1];

        for (int i = 0; i <= numSegments; i++)
        {
            float theta = i * segmentAngle;
            float x = Mathf.Cos(theta) * radius;
            float z = Mathf.Sin(theta) * radius;

            topVertices[i] = new Vector3(x, height / 2, z);
            bottomVertices[i] = new Vector3(x, -height / 2, z);
        }

        // 绘制顶部和底部的圆
        for (int i = 0; i < numSegments; i++)
        {
            Gizmos.DrawLine(topVertices[i], topVertices[i + 1]);
            Gizmos.DrawLine(bottomVertices[i], bottomVertices[i + 1]);
        }

        // 连接顶部和底部的对应点，形成圆柱体的侧面
        for (int i = 0; i <= numSegments; i++)
        {
            Gizmos.DrawLine(topVertices[i], bottomVertices[i]);
        }

        Gizmos.color = Color.blue;
        for (int i = 0; i <= numSegments / 4; i++) // 假设扇形是1/4圆，调整numSegments/4以适应实际角度
        {
            int index = (int)(i * (numSegments / (angle / 360.0f))); // 根据扇形角度计算索引
            Gizmos.DrawLine(Vector3.zero, topVertices[index]); // 从中心到顶部顶点

            if (index < numSegments)
            {
                int nextIndex = (index + (int)(numSegments / (angle / 90.0f))) % numSegments;
                Gizmos.DrawLine(topVertices[index], topVertices[nextIndex]);
            }
        }
    }

}