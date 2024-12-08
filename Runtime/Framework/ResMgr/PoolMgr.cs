using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 放置公用的Path对象池
    /// </summary>
    public class PoolMgr : Singleton<PoolMgr>
    {
        //借助 
        private Dictionary<string, AssetPool> dicPools = new Dictionary<string, AssetPool>();

        private Dictionary<GameObject, AssetPool> prefabPools = new Dictionary<GameObject, AssetPool>();

        public GameObject Get(string path, float releaseTime = 0)
        {
            GameObject obj = GetOrCreatPool(path).Get();
            if (releaseTime > 0)
            {
                var task = XCTime.DelayRun(releaseTime, () => { Release(path, obj); });
            }
            return obj;
        }

        public GameObject GetFromPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("---  prefab null");
                return null;
            }
            return GetOrCreatPrefabPool(prefab).Get();
        }

        private AssetPool GetOrCreatPrefabPool(GameObject prefab)
        {
            var id = prefab.GetInstanceID().ToString();

            if (!dicPools.ContainsKey(id))
            {
                AssetPool pool = new AssetPool(prefab);
                dicPools[id] = pool;
                return pool;
            }
            else
            {
                return dicPools[id];
            }
        }

        public void Release(string path, GameObject obj)
        {
            GetOrCreatPool(path).Release(obj);
        }

        /// <summary>
        /// 可在初始化的时候提前加载
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private AssetPool GetOrCreatPool(string path)
        {
            if (dicPools.ContainsKey(path))
            {
                return dicPools[path];
            }
            AssetPool assetPool = new AssetPool(path);

            dicPools[path] = assetPool;

            return assetPool;
        }

        public void ClearAllPool()
        {
            foreach (var pool in dicPools.Values)
            {
                pool.pool.Clear();
                dicPools.Remove(pool.path);
            }
        }


        /// <summary>
        /// GameObject对象池  示例
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public void CreatPefabPoolExample(GameObject prefab)
        {
            //创建
            AssetPool pool = new AssetPool(prefab);
            //获取
            GameObject newObj = pool.Get();
            //释放
            pool.Release(newObj);
        }

        //TODO循环池 
        public void CreatLoopPoolExample(List<Object> obj)
        {

        }

    }
}