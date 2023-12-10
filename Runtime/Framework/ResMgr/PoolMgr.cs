using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// GameRuning
    /// </summary>
    public class PoolMgr : Singleton<PoolMgr>
    {
        //借助 
        private Dictionary<string, AssetPool> dicPools = new();

        public void Release(string path, GameObject obj)
        {
            ReleaseOne(GetOrCreatPool(path), obj);
        }

        private void ReleaseOne(AssetPool pool, GameObject obj)
        {
            pool.pool.Release(obj);
        }

        public GameObject Get(string path, float releaseTime = 0)
        {
            GameObject obj = GetOrCreatPool(path).pool.Get();
            if (releaseTime > 0)
            {
                var task = XCTime.DelayRun(releaseTime, () => { Release(path, obj); });
            }
            return obj;
        }
        /// <summary>
        /// 可在初始化的时候提前加载
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public AssetPool GetOrCreatPool(string path)
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
        /// 定制对象池  示例
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public void CreatPefabPool(GameObject prefab)
        {
            AssetPool pool = new AssetPool(prefab);
            GameObject newObj = pool.pool.Get();
            pool.pool.Release(newObj);
        }



    }
}