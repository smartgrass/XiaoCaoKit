using System;
using NaughtyAttributes;
using System.Collections.Generic;
using GG.Extensions;
using UnityEngine;
using XiaoCao;
using XiaoCao.Map;

public class Test_WallLayout : MonoBehaviour
{
    public Test_WallCell prefab;

    public GameObject PrefabBody => prefab.BodyPrefab;
    public GameObject PrefabNode => prefab.NodePrefab;
    public float BodyWidth => prefab.bodyWidth;
    public float NodeOffset => prefab.nodeOffset;

    public float nodeWidth => prefab.nodeWidth;

    //围墙的结构分为body(墙体)和node(连接点),
    //可以输入长度len,生成对应长度的围墙.
    //body与body间隔为bodyWidth,连接点间隔为nodeOffset
    //连接结构如下:body1-body2-body3-node1-body1-body2-body3-node2

    public int bodyCount = 3;

    public int len = 5;

    [Button("生成")]
    public void Gen()
    {
        transform.DestroyChildren();

        // 根据描述，我们需要按照特定模式生成围墙:
        // body1-body2-body3-node1-body1-body2-body3-node2...
        // 即每组包含3个body和1个node

        float currentPosition = -BodyWidth;
        int placedCount = 0; // 已放置的总元素数（包括body和node）

        while (placedCount < len)
        {
            // 先放置3个body
            for (int i = 0; i < bodyCount && placedCount < len; i++)
            {
                GameObject body = BuildingTool.EditorInstancePrefab(PrefabBody, transform);
                currentPosition += BodyWidth;
                body.transform.localPosition = new Vector3(currentPosition, 0, 0);
                placedCount++;
            }

            // 如果还没达到总长度，放置一个node
            if (placedCount < len)
            {
                GameObject node = BuildingTool.EditorInstancePrefab(PrefabNode, transform);
                node.transform.localPosition = new Vector3(currentPosition + NodeOffset, 0, 0);
                currentPosition += nodeWidth;
            }
        }
    }


    [Button]
    void AddBoxCollider()
    {
        BuildingTool.AddBoxCollider(transform);
    }
}