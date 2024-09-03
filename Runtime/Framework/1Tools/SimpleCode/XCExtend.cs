using System;
using System.Text.RegularExpressions;
using UnityEngine;
using XiaoCao;

public static class XCExtend
{
    public static bool TryPlayAnim(this Animator animator , int hash ,int layer = 0)
    {
        if (animator.HasState(layer, hash))
        {
            animator.Play(hash);
            return true;
        }
        Debuger.LogError($"--- no hash {hash}");
        return false;
    }

    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T ret = go.GetComponent<T>();
        if (null == ret)
            ret = go.AddComponent<T>();
        return ret;
    }

    public static Transform FindOrNewChildren(this Transform tf, string childrenName)
    {
        Transform ret = tf.Find(childrenName);
        if (ret == null)
        {
            var newObject = new GameObject(childrenName);
            newObject.transform.SetParent(tf, false);
        }
        return ret;
    }

    // 递归方法来查找子物体
    public static Transform FindChildEx(this Transform parent, string name)
    {

        // 遍历所有子物体
        int len = parent.childCount;
        for (int i = 0; i < len; i++)
        {
            Transform child = parent.GetChild(i);

            // 忽略大小写
            if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            Transform result = FindChildEx(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }


    /// <summary>
    /// 缓慢旋转到目标位置方向 , 并且返回是否到达
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="targetPos"></param>
    /// <param name="rotationSpeed"></param>
    /// <param name="minDetal"></param>
    /// <returns></returns>
    public static bool RoateY_Slow(this Transform transform, Vector3 targetPos, float rotationSpeed, float minDetal = 1)
    {
        Vector3 direction = targetPos - transform.position;
        direction.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed);

        float angle = Quaternion.Angle(transform.rotation, targetRotation);

        if (Mathf.Abs(angle) <= minDetal)
        {
            //到达
            //可选择强制对准 transform.RotaToPos(targetPos);
            return true;
        }
        else
        {
            return false;
        }
    }

}
