using Fantasy.Pool;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
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

        public AssetPool GetOrCreatPrefabPool(GameObject prefab)
        {
            //var id = prefab.GetInstanceID().ToString();
            if (!prefabPools.ContainsKey(prefab))
            {
                prefabPools[prefab] = new AssetPool(prefab);
            }
            return prefabPools[prefab];
        }

        public GameObject Get(string path, float releaseTime = 0)
        {
            GameObject obj = GetOrCreatPool(path).Get();
            if (releaseTime > 0)
            {
                var task = XCTime.DelayTimer(releaseTime, () => { Release(path, obj); });
            }
            return obj;
        }


        public void Release(string path, GameObject obj)
        {
            GetOrCreatPool(path).Release(obj);
        }



        public void ClearAllPool()
        {
            var keys = dicPools.Keys;
            foreach (var pool in dicPools.Values)
            {
                ClearPool(pool);
            }
            dicPools.Clear();

            foreach (var pool in prefabPools.Values)
            {
                ClearPool(pool);
            }
            prefabPools.Clear();
        }

        private void ClearPool(AssetPool pool)
        {
            pool.pool.Clear();
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


    /// <summary>
    /// 生成代码用
    /// </summary>
    public interface IGameObjectPoolExample
    {
        public GameObject GetFromPool();
        public void Release(GameObject obj);
    }
}