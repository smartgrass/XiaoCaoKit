using Flux;
using System;
using UnityEngine;
using XiaoCao;
/*
 攻击层级, PLAYER_ATK & Layers.ENEMY_ATK
 受击中层级:  PLAYER & ENEMY

通过Trigger接受信息
 */

public class MsgTrigger : MonoBehaviour
{
    public Action<Collider> triggerEnterAct;

    private void OnTriggerEnter(Collider other)
    {
        triggerEnterAct?.Invoke(other);
    }

    //TODO 碰撞抽取

    public void ClearListener()
    {

    }
}

public class BoxRayCaster : MonoBehaviour
{
    public MeshInfo meshInfo;

    public int layerMask;

    public Action<Collider> triggerEnterAct;

    public MeshType MeshType => meshInfo.meshType;


    RaycastHit[] hits = new RaycastHit[16];

    ///<see cref="XCTriggerEvent"/>
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
    private void OnBox()
    {
        Vector3 center = transform.TransformPoint(meshInfo.GetCenter);
        int hitCount = Physics.BoxCastNonAlloc(center, meshInfo.GetSize / 2, meshInfo.GetEulerAngles, hits);
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                triggerEnterAct?.Invoke(hits[i].collider);
            }
        } 
    }

    private void OnSector()
    {
        Vector3 center = transform.TransformPoint(meshInfo.GetCenter);
        int hitCount = Physics.SphereCastNonAlloc(center, meshInfo.GetRadius, meshInfo.GetEulerAngles, hits);
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Vector3 hitPoint = hits[i].point;
                float hight = meshInfo.GetHight;
                float angleRange = meshInfo.GetRadian;


                triggerEnterAct?.Invoke(hits[i].collider);
            }
        }

#if UNITY_EDITOR
        //ForTest

#endif
    }

    private void OnSphere()
    {
        Vector3 center = transform.TransformPoint(meshInfo.GetCenter);
        int hitCount = Physics.BoxCastNonAlloc(center, meshInfo.GetSize / 2, meshInfo.GetEulerAngles, hits);
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                triggerEnterAct?.Invoke(hits[i].collider);
            }
        }
    }


}