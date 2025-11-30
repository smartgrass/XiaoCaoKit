using Flux;
using RotaryHeart.Lib.PhysicsExtension;
using System;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;
using Color = UnityEngine.Color;
using Physics = RotaryHeart.Lib.PhysicsExtension.Physics;

/// <summary>
/// 碰撞射线检测
/// 不足:扇形柱无法精确检测
/// </summary>
public class RayCasterTrigger : MonoBehaviour, ITrigger
{
    public MeshInfo meshInfo;

    public int layerMask = Layers.DEFAULT;

    public Action<Collider> TriggerAct { get; set; }

    public MeshType MeshType => meshInfo.meshType;

    RaycastHit[] hits = new RaycastHit[16];

    public HashSet<Collider> tempColliders = new HashSet<Collider>();

    private float distance = 0;

    //与Atker在同个GameObject, 坐标要从本地坐标换算
    //旋转方向也需要从本地旋转中叠加
    Vector3 WorldCenter
    {
        get { return transform.TransformPoint(meshInfo.GetCenter); }
    }

    Vector3 WorldEulerAngles
    {
        get { return transform.eulerAngles + meshInfo.GetEulerAngles; }
    }

    Vector3 Direction
    {
        get { return Quaternion.Euler(WorldEulerAngles) * Vector3.right; }
    }

    private Vector3 SelfSize => transform.lossyScale;

    private Vector3 lastPoint;
    private PreviewCondition preview = PreviewCondition.Editor;

    public void SetMeshInfo(MeshInfo meshInfo)
    {
        this.meshInfo = meshInfo;
        transform.localScale = Vector3.one;
    }

    public void InitListener(Action<Collider> action,int atkTeam)
    {
        layerMask = XCSetting.GetTeamInverseLayerMask(atkTeam);
        TriggerAct = null;
        TriggerAct += action;
        tempColliders.Clear();
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        lastPoint = WorldCenter;
    }

    public void Switch(bool isOn)
    {
        enabled = isOn;
        tempColliders.Clear();
    }

    public void OnFinish()
    {
        Switch(false);
    }


    private void FixedUpdate()
    {
        switch (MeshType)
        {
            case MeshType.Box:
                BoxLine();
                break;
            case MeshType.Sphere:
                OnSphere();
                break;
            case MeshType.Sector:
                OnSector();
                break;
            default:
                break;
        }
    }

    private void DoTrigger(Collider collider)
    {
        if (!tempColliders.Contains(collider))
        {
            TriggerAct?.Invoke(collider);
            tempColliders.Add(collider);
        }
    }

    private void ClearTemp()
    {
        tempColliders.Clear();
    }

    private void BoxLine()
    {
        float curDistance = 0;
        var dir = (WorldCenter - lastPoint);
        if (dir.IsZore())
        {
            curDistance = 0;
        }
        else
        {
            curDistance = dir.magnitude;
        }

        OnBox(curDistance);

        lastPoint = WorldCenter;
    }

    private void OnBox(float dis = 0)
    {
        var selfSize = SelfSize;
        var size = new Vector3(
            selfSize.x * meshInfo.GetSize.x,
            selfSize.y * meshInfo.GetSize.y,
            selfSize.z * meshInfo.GetSize.z
        ) * 0.5f;
        int hitCount = Physics.BoxCastNonAlloc(WorldCenter, size, Direction,
            hits,
            Quaternion.Euler(WorldEulerAngles), dis, layerMask, preview: preview);
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
        int hitCount = Physics.SphereCastNonAlloc(WorldCenter, meshInfo.GetRadius * SelfSize.x, Direction, hits,
            distance, layerMask,
            preview);
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                DoTrigger(hits[i].collider);
            }
        }
    }


    /// <summary>
    /// 只能做球的角度范围检测, 高度检测无法通过HitPoint计算
    /// </summary>
    private void OnSector()
    {
        int hitCount = Physics.SphereCastNonAlloc(WorldCenter, meshInfo.GetRadius * SelfSize.x, Direction, hits,
            distance, layerMask,
            preview: preview);
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Vector3 hitPoint = hits[i].point;
                float angleRange = meshInfo.GetRadian;

                //算出点在扇形柱空间的局部坐标
                Quaternion rotationQuaternion = Quaternion.Euler(WorldEulerAngles);
                Vector3 localPoint = Quaternion.Inverse(rotationQuaternion) * (hitPoint - WorldCenter);

                //float hight = meshInfo.GetHight;
                //float heightCheck = Mathf.Abs(localPoint.y); // 在局部坐标系中，y轴代表高度
                //bool isWithinCylinder = (heightCheck <= hight / 2);

                if (IsPointInAngelRange(localPoint, angleRange))
                {
                    DoTrigger(hits[i].collider);
                }
            }
        }
#if UNITY_EDITOR
        //ForTest Forward
        TestOnSector();
#endif
    }

    void TestOnSector()
    {
        float testAngleRange = meshInfo.GetRadian;
        Physics.Raycast(WorldCenter, Direction, meshInfo.GetRadius + 1, preview: preview);
        for (int i = 0; i < 5; i++)
        {
            float angle = -testAngleRange / 2 + i * testAngleRange / 4;
            var dir = MathTool.RotateY(Vector3.right, angle);
            Physics.Raycast(WorldCenter, Quaternion.Euler(WorldEulerAngles) * dir, meshInfo.GetRadius + 1,
                preview: preview);
        }
    }

    static bool IsPointInAngelRange(Vector3 localPoint, float angle)
    {
        // 计算点相对于圆心的角度（在xz平面上，从x轴正方向逆时针测量）
        float angleToPoint = Mathf.Atan2(localPoint.z, localPoint.x);

        // 将角度标准化到 [0, 2*PI)
        if (angleToPoint < 0)
        {
            angleToPoint += 2 * Mathf.PI;
        }

        // 假设扇形从x轴正方向开始，逆时针测量角度范围
        float startAngle = -angle / 2; // 可以根据需要修改这个值
        float endAngle = startAngle + angle;
        if (endAngle > 2 * Mathf.PI)
        {
            endAngle -= 2 * Mathf.PI;
        }

        Debug.Log($"--- angleToPoint {angleToPoint} {startAngle} {endAngle}");

        // 检查点是否在扇形角度范围内
        bool isWithinSector = (angleToPoint >= startAngle && angleToPoint <= endAngle) ||
                              (endAngle < startAngle && (angleToPoint >= startAngle || angleToPoint <= endAngle));
        return isWithinSector;
    }
}