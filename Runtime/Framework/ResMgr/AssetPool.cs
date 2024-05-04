using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace XiaoCao
{
    /// <summary>
    /// 对象池, 需要手动回收
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

            if (prefab == null)
            {
                //Cube代替
                prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Debug.LogError($"no Asset!! {path}");
#if UNITY_EDITOR
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                //编辑器兼容大小写,可能是大小写的问题
                Debug.LogError($"Editor test has ? {obj!=null}");
#endif
            }

            pool = new ObjectPool<GameObject>(Creat, OnCreat, OnRelease, GetID);
        }

        public GameObject Get()
        {
            return pool.Get();
        }
        public void Release(GameObject gameObject)
        {
            pool.Release(gameObject);
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