using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// GameObject 使用yooAsset加载对象
    /// </summary>
    public class AssetPool
    {
        public const string PoolName = "Pool";
        public string path;
        public GameObject prefab;
        public ObjectPool<GameObject> pool;

        public AssetPool(GameObject prefab)
        {
            this.prefab = prefab;
            prefab.SetActive(false);
            pool = new ObjectPool<GameObject>(Creat, OnCreat, OnRelease, GetID);
        }

        public AssetPool(string path)
        {
            this.path = path;
            var handle = ResMgr.Loader.LoadAssetSync<GameObject>(path);
            prefab = handle.AssetObject as GameObject;

            if (prefab != null)
            {
                throw new System.Exception($"no Asset GameObject {path}");
            }

            pool = new ObjectPool<GameObject>(Creat, OnCreat, OnRelease, GetID);
        }

        private GameObject Creat()
        {
            Transform transform = DontDestroyTransfrom.Get(PoolName);
            GameObject newObj = Object.Instantiate(prefab, transform);
            return newObj;
        }

        private int GetID(GameObject instance)
        {
            return instance.GetInstanceID();
        }

        private void OnRelease(GameObject obj)
        {
            obj.SetActive(false);
        }

        private void OnCreat(GameObject obj)
        {
            obj.SetActive(true);
        }
    }
}