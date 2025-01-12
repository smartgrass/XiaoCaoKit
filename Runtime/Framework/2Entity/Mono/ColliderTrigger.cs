using Flux;
using ProtoBuf.Meta;
using System;
using System.Drawing;
using UnityEngine;
using XiaoCao;
/*
 攻击层级, PLAYER_ATK & Layers.ENEMY_ATK
 受击中层级:  PLAYER & ENEMY

通过Trigger接受信息
 */

///<see cref="RayCasterTrigger"/>
public class ColliderTrigger : MonoBehaviour, ITrigger
{
    public Action<Collider> TriggerAct { get; set; }

    void OnTriggerEnter(Collider other)
    {
        TriggerAct?.Invoke(other);
    }

    public void InitListener(Action<Collider> action)
    {
        TriggerAct = null;
        TriggerAct += action; 
    }

    public void Switch(bool isOn)
    {
        enabled = isOn;
    }


    public void OnFinish()
    {
        Switch(false);
    }

    public void SetMeshInfo(MeshInfo meshInfo)
    {
        switch (meshInfo.meshType)
        {
            case MeshType.Box:
                OnBox(meshInfo);
                break;
            case MeshType.Sphere:
                OnSphere(meshInfo);
                break;
            case MeshType.Sector:
                OnSector(meshInfo);
                break;
            default:
                Debug.LogError($"--- none {meshInfo.meshType}");
                break;
        }

    }

    private void OnBox(MeshInfo meshInfo)
    {
        var col = gameObject.GetOrAddComponent<BoxCollider>();
        col.isTrigger = true;
        var tf = gameObject.transform;
        tf.localScale = meshInfo.GetSize;
        tf.localPosition = meshInfo.GetCenter;
        tf.localRotation = Quaternion.Euler(meshInfo.GetEulerAngles);
        col.center = Vector3.zero;
        col.size = Vector3.one;
    }

    private void OnSphere(MeshInfo meshInfo)
    {
        var col = gameObject.GetOrAddComponent<SphereCollider>();
        col.isTrigger = true;
        var tf = gameObject.transform;

        col.radius = 1;
        tf.localScale = meshInfo.GetSize;
        tf.localPosition = meshInfo.GetCenter;
        tf.localRotation = Quaternion.Euler(meshInfo.GetEulerAngles);
    }

    public void OnSector(MeshInfo meshInfo)
    {
        var col = gameObject.GetOrAddComponent<MeshCollider>();

        col.convex = true;
        col.isTrigger = true;
        col.sharedMesh = MathLayoutTool.GetSectorMesh(meshInfo.GetRadian, meshInfo.GetRadius, meshInfo.GetHight, 20);
        var tf = gameObject.transform;

        tf.localScale = Vector3.one;
        tf.localPosition = meshInfo.GetCenter;
        tf.localRotation = Quaternion.Euler(meshInfo.GetEulerAngles);
    }
}

public interface ITrigger
{
    public Action<Collider> TriggerAct { get; set;}

    void InitListener(Action<Collider> action);

    void SetMeshInfo(MeshInfo meshInfo);

    void Switch(bool v);

    void OnFinish();

}
