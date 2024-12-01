using NaughtyAttributes;
using System;
using UnityEngine;

public class Test_Shadow_Body : MonoBehaviour
{
    Transform tempTransfrom;
    // 玩家预制体的引用
    public GameObject playerPrefab;
    // 新的材质的引用
    public Material newMaterial;
    // 动画剪辑的引用
    public AnimationClip animationClip;

    public float time;

    [Button]
    void Creat()
    {
        tempTransfrom = this.transform;
        CreatePlayerWithCustomMaterialAndPose();
    }

    GameObject CreatePlayerWithCustomMaterialAndPose()
    {
        // 创建玩家物体
        GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerInstance.transform.SetParent(tempTransfrom, true);
        Renderer[] renderers = playerInstance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("Player prefab does not have a Renderer component!");
            return null;
        }

        // 获取玩家物体的 Animation 组件并设置动画剪辑
        Animator animation = playerInstance.GetComponent<Animator>();
        if (animation == null)
        {
            Debug.LogError("Player prefab does not have an Animation component!");
            return null;
        }

        foreach (var renderer in renderers)
        {
            SetMat(renderer);
        }

        // 强制动画状态立即更新到第一帧
        animationClip.SampleAnimation(playerInstance, time);
        return playerInstance;
    }


    private void SetMat(Renderer renderer)
    {
        if (Application.isPlaying)
        {
            if (renderer.materials.Length > 1)
            {
                Material[] mats = new Material[renderer.materials.Length];
                Array.Fill(mats, newMaterial);
                renderer.materials = mats;
            }
            else
            {
                renderer.material = newMaterial;
            }
        }
        else
        {
            if (renderer.sharedMaterials.Length > 1)
            {
                Material[] mats = new Material[renderer.sharedMaterials.Length];
                Array.Fill(mats, newMaterial);
                renderer.sharedMaterials = mats;
            }
            else
            {
                renderer.sharedMaterial = newMaterial;
            }
        }
    }
}
