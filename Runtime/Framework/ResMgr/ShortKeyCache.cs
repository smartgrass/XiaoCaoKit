using UnityEngine;
using YooAsset;

public class ShortKeyCache
{
    public GameObject prefab;
    public string path;
    public ResourcePackage package;

    /// <summary>
    /// 按短键缓存加载预制体, 加载失败时返回 null 并打印失败原因
    /// </summary>
    public GameObject LoadPrefab()
    {
        if (prefab == null)
        {
            if (package.IsLocationValid(path))
            {
                var handle = package.LoadAssetSync<GameObject>(path);
                prefab = handle.AssetObject as GameObject;
                if (prefab == null)
                {
                    Debug.LogError($"--- load failed {path} at {package.PackageName} status:{handle.Status} error:{handle.Error}");
                }
            }
            else
            {
                Debug.LogError($"--- no {path} at {package.PackageName}");
                prefab = ResMgr.LoadPrefab(path, PackageType.DefaultPackage);
            }
        }
        return prefab;
    }
}
