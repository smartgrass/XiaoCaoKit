using UnityEngine;
using YooAsset;

public class ShortKeyCache
{
    public GameObject prefab;
    public string path;
    public ResourcePackage package;

    public GameObject LoadPrefab()
    {
        if (prefab == null)
        {
            if (package.CheckLocationValid(path))
            {
                prefab = package.LoadAssetSync<GameObject>(path).AssetObject as GameObject;
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
