using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 缓存池,只增不减 ,适应于UI格子
/// </summary>
public class CachePool<T> where T : MonoBehaviour
{
    private void Example()
    {
        var pool = new CachePool<T>(prefab);

        int needCount = 10;
        pool.UpdateCachedAmount(needCount);
        for (int i = 0; i < needCount; i++)
        {
            Debug.Log(pool.cacheList[i]);
        }
    }

    public List<T> cacheList = new List<T>();
    private GameObject prefab;
    private int cachedAmount;

    public CachePool(GameObject prefab)
    {
        this.prefab = prefab;
        this.cachedAmount = 0;
    }


    public void UpdateCachedAmount(int newAmount)
    {
        cachedAmount = newAmount;

        // 如果当前对象数量小于缓存数量，生成新的对象
        while (cacheList.Count < cachedAmount)
        {
            T item = CreatNew();
            cacheList.Add(item);
        }

        // 隐藏多余的对象
        for (int i = cachedAmount; i < cacheList.Count; i++)
        {
            cacheList[i].gameObject.SetActive(false);
        }
    }

    private T CreatNew()
    {
        return Object.Instantiate(prefab).GetComponent<T>();
    }
}
