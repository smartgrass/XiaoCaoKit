using UnityEngine;
using System.Collections;
using XiaoCao;
using System.Collections.Generic;
using Flux;
using System.Drawing;
using Color = UnityEngine.Color;
using System;

public class TriggerDrawer : MonoBehaviour
{
    public bool IsShow = true;

    public int frame = 0;

    public Color drawColor;

    public Dictionary<int, FTriggerRangeTrack> tracks = new Dictionary<int, FTriggerRangeTrack>();

    public void Regist(FTriggerRangeTrack track)
    {
        if (!tracks.ContainsKey(track.GetInstanceID()))
        {
            tracks.Add(track.GetInstanceID(), track);
        }
    }


    public void Remove(FTrack track)
    {
        if (tracks.ContainsKey(track.GetInstanceID()))
        {
            tracks.Remove(track.GetInstanceID());
        }
    }

    public void Clear()
    {
        tracks.Clear();
    }

    private void OnDrawGizmos()
    {
        if (!IsShow)
        {
            return;
        }

        foreach (FTriggerRangeTrack track in tracks.Values)
        {
            if (track == null)
                continue;
            foreach (var item in track.Events)
            {
                var ev = (FTriggerRangeEvent)item;
                if (item.FrameRange.Contains(frame))
                {
                    //Draw
                    DrawMesh(ev.meshInfo, ev.Owner);
                }
                else
                {
                    //UnDraw
                }
            }
        }

    }
    void DrawMesh(MeshInfo meshInfo, Transform targetTran)
    {
        var rotation = targetTran.rotation;
        var angle = rotation.eulerAngles;
        var center = meshInfo.GetCenter;
        var size = meshInfo.GetSize;

        Gizmos.color = drawColor;
        //坐标系选为targetTran
        Gizmos.matrix = Matrix4x4.TRS(targetTran.position, Quaternion.Euler(angle), targetTran.lossyScale);

        if (meshInfo.meshType == MeshType.Box)
        {
            Gizmos.DrawWireCube(center, size);
        }
        else if (meshInfo.meshType == MeshType.Sphere)
        {
            Gizmos.DrawWireSphere(center, size.x);
        }
        else if (meshInfo.meshType == MeshType.Sector)
        {
            Mesh mesh = MathLayoutTool.GetSectorMesh(meshInfo.GetRadian, meshInfo.GetRadius, meshInfo.GetHight, 20);
            Quaternion rota = Quaternion.Euler(meshInfo.GetEulerAngles);
            Gizmos.DrawWireMesh(mesh, meshInfo.GetCenter, rota, Vector3.one);
        }
        else
        {
            Mesh mesh = new Mesh();
            mesh.vertices = meshInfo.values;
            Gizmos.DrawWireMesh(mesh);
        }
        Gizmos.matrix = Matrix4x4.identity;

    }
}
