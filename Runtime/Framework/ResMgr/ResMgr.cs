using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using XiaoCao;
using YooAsset;
using Debug = UnityEngine.Debug;

public class ResMgr
{
    public static void LoadExample(string path)
    {
        //加载 1
        GameObject go = GameObject.Instantiate(LoadPrefab(path));
        //加载 2
        go = PoolMgr.Inst.Get(path);

        /*
        https://www.yooasset.com/docs/guide-runtime/CodeTutorial3
        https://www.yooasset.com/docs/api/YooAsset/ResourcePackage
        */
    }

    public const string DefaultPackage = "DefaultPackage";
    public const string RawPackage = "RawPackage";

    ///<see cref="GetExtraPackageUrl"/>
    public const string ExtraPackage = "ExtraPackage"; //用于Mod

    public const string RESDIR = "Assets/_Res";

    public static bool IsLoadFinish;
    public static ResourcePackage Loader;
    public static ResourcePackage RawLoader;

    public static Dictionary<string, ShortKeyCache> ShortKeyDic = new Dictionary<string, ShortKeyCache>();

    public static GameObject LoadInstan(string path, PackageType type = PackageType.DefaultPackage)
    {
        return GameObject.Instantiate(LoadPrefab(path, type));
    }

    ///<see cref="ConfigMgr.GetSkinList"/>
    public static GameObject TryShorKeyInst(string shortKey, string failBackPath)
    {
        if (ResMgr.ShortKeyDic.ContainsKey(shortKey))
        {
            var prefab = ResMgr.ShortKeyDic[shortKey].LoadPrefab();
            return GameObject.Instantiate(prefab);
        }
        else
        {
            return ResMgr.LoadInstan(failBackPath, PackageType.DefaultPackage);
        }
    }


    //只加载, 没有实例化
    public static GameObject LoadPrefab(string path, PackageType type = PackageType.DefaultPackage)
    {
        var task = Loader.LoadAssetSync<GameObject>(path);
        return task.AssetObject as GameObject;

        //if (ExtraLoader.CheckLocationValid(path))
        //{
        //    Debug.Log($"--- ExtraLoader {path}");
        //    return ExtraLoader.LoadAssetSync<GameObject>(path).AssetObject as GameObject;
        //}
        //else
        //{
        //    Debug.LogWarning($"--- no Extra FailBack to Default {path}");
        //return LoadPrefab(path, PackageType.DefaultPackage);
        //}
    }

    public static AssetHandle LoadPrefabAsyncHandle(string path)
    {
        return Loader.LoadAssetAsync<GameObject>(path);
    }

    public static T LoadAseetOrDefault<T>(string path, string fallBackPath) where T : Object
    {
        T ret = LoadAseet<T>(path);
        if (ret)
        {
            return ret;
        }
        return LoadAseet<T>(fallBackPath);
    }

    public static T LoadAseet<T>(string path) where T : Object
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Debug.Log($"--- Application.isPlaying false");
            return null;
        }
#endif

        if (Loader.CheckLocationValid(path))
        {
            var task = Loader.LoadAssetSync<T>(path);
            return task.AssetObject as T;
        }
        else
        {
            Debug.LogWarning($"--- no Asset {path}");
            return null;
        }
    }

    //只加载, 没有实例化
    public static Object LoadAseet(string path)
    {
        if (Loader.CheckLocationValid(path))
        {
            var task = Loader.LoadAssetSync(path);
            return task.AssetObject;
        }
        else
        {
            Debug.LogWarning($"--- no Asset {path}");
            return null;
        }
    }

    public static byte[] LoadRawByte(string path)
    {
        Debug.Log($"---  {path}");
        if (!RawLoader.CheckLocationValid(path))
        {
            return null;
        }

        var handle = RawLoader.LoadRawFileSync(path);
        byte[] fileData = handle.GetRawFileData();
        return fileData;
    }

    #region Init
    public static async UniTask InitYooAssetAll()
    {
        ResMgr.InitYooAsset();
        ShortKeyDic.Clear();
        var task1 = ResMgr.InitDefaultPackage(); ;
        var task2 = ResMgr.InitRawPackage();
        var task3 = ResMgr.InitExtraPackage();
        //使用并行任务,相比同步快个300ms
        await UniTask.WhenAll(task1, task2, task3);
        IsLoadFinish = true;
    }

    public static void InitYooAsset()
    {
        // 初始化资源系统
        YooAssets.Initialize();
        // 创建默认的资源包
        Loader = YooAssets.CreatePackage(DefaultPackage);
        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(Loader);
    }

    public static async UniTask InitDefaultPackage()
    {
        string packageName = DefaultPackage;
        Debug.Log($"--- InitPackage {packageName}");
        ResourcePackage package = GetOrCreatPackage(packageName);
        InitializationOperation initOperation = null;
        EPlayMode playMode = DebugSetting.GetEPlayMode();
        Loader = package;

#if UNITY_EDITOR
        //编辑器模式使用。
        Debuger.LogWarning($"编辑器模式使用:{playMode}");
        // 编辑器下的模拟模式
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var buildPipeline = EDefaultBuildPipeline.BuiltinBuildPipeline;
            var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(buildPipeline, DefaultPackage);
            var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult);
            var initParameters = new EditorSimulateModeParameters();
            initParameters.EditorFileSystemParameters = editorFileSystem;
            initOperation = package.InitializeAsync(initParameters);
            Debug.Log($"--- EditorSimulateMode");
        }
#endif
        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var initParameters = new OfflinePlayModeParameters();
            initParameters.BuildinFileSystemParameters = buildinFileSystemParams;
            initOperation = package.InitializeAsync(initParameters);
        }

        await initOperation;

        var operation1 = package.RequestPackageVersionAsync();
        await operation1;
        var operation2 = package.UpdatePackageManifestAsync(operation1.PackageVersion);
        await operation2;
        Debug.Log($"--- InitPackageEnd {packageName}");
    }


    public static async UniTask InitExtraPackage()
    {
        foreach (IniSection section in ConfigMgr.Inst.MainCfg.SectionList)
        {
            if (section.SectionName.StartsWith("Mod"))
            {
                var packageName = section.SectionName;
                ResourcePackage package = YooAssets.CreatePackage(packageName);

                InitializationOperation initOperation = null;
                string defaultHostServer = GetExtraPackageUrl(packageName, out bool hasManifest);
                if (!hasManifest)
                {
                    Debug.LogWarning($"--- continue No Package {packageName} {defaultHostServer}");
                    continue;
                }

                Debug.Log($"--- InitPackage {packageName} {defaultHostServer}");
                string fallbackHostServer = defaultHostServer;

                IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                var cacheFileSystem = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                var initParameters = new HostPlayModeParameters();
                initParameters.BuildinFileSystemParameters = cacheFileSystem;
                initParameters.CacheFileSystemParameters = cacheFileSystem;

                initOperation = package.InitializeAsync(initParameters);

                await UpdatePackage(package, initOperation);
                Debug.Log($"--- InitPackageEnd {packageName}");

                foreach (var kv in section.Dic)
                {
                    ShortKeyDic[kv.Key] = new ShortKeyCache()
                    {
                        package = package,
                        path = kv.Value
                    };
                }
            }
        }
    }


    public static async UniTask InitRawPackage()
    {
        EPlayMode playMode = DebugSetting.GetEPlayMode();

        string packageName = RawPackage;

        Debug.Log($"--- InitPackage {packageName}");

        ResourcePackage package = GetOrCreatPackage(packageName);

        RawLoader = package;

        InitializationOperation initOperation = null;

#if UNITY_EDITOR
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            //注意：如果是原生文件系统选择EDefaultBuildPipeline.RawFileBuildPipeline
            var buildPipeline = EDefaultBuildPipeline.RawFileBuildPipeline;
            var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(buildPipeline, RawPackage);
            var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult);
            var initParameters = new EditorSimulateModeParameters();
            initParameters.EditorFileSystemParameters = editorFileSystem;
            initOperation = package.InitializeAsync(initParameters);
        }
#endif

        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinRawFileSystemParameters();
            var initParameters = new OfflinePlayModeParameters();
            initParameters.BuildinFileSystemParameters = buildinFileSystem;
            initOperation = package.InitializeAsync(initParameters);
        }

        await UpdatePackage(package, initOperation);
        Debug.Log($"--- InitPackageEnd {packageName}");
    }

    private static async Task UpdatePackage(ResourcePackage package, InitializationOperation initOperation)
    {
        await initOperation.Task;
        var operation1 = package.RequestPackageVersionAsync();
        await operation1;
        var operation2 = package.UpdatePackageManifestAsync(operation1.PackageVersion);
        await operation2;
    }

    private static ResourcePackage GetOrCreatPackage(string packageName)
    {
        var tempPack = YooAssets.TryGetPackage(packageName);
        if (tempPack == null)
        {
            tempPack = YooAssets.CreatePackage(packageName);
        }

        return tempPack;
    }

    private static string GetExtraPackageUrl(string packName, out bool hasManifest)
    {
        string path = $"{XCPathConfig.GetExtraPackageDir()}/{packName}/";

        hasManifest = HasManifest(path, packName);

        return $"file://{path}";

    }

    private static bool HasManifest(string dir, string packName)
    {
        string fileName = $"PackageManifest_{packName}.version";
        string filePath = System.IO.Path.Combine(dir, fileName);

        bool ret = FileTool.IsFileExist(filePath);
        return ret;
    }

    #endregion


    #region ShortKey&Mod


    #endregion
}


/// <summary>
/// 远端资源地址查询服务类
/// </summary>
public class RemoteServices : IRemoteServices
{
    private readonly string _defaultHostServer;
    private readonly string _fallbackHostServer;

    public RemoteServices(string defaultHostServer, string fallbackHostServer)
    {
        _defaultHostServer = defaultHostServer;
        _fallbackHostServer = fallbackHostServer;
    }

    string IRemoteServices.GetRemoteMainURL(string fileName)
    {
        return $"{_defaultHostServer}/{fileName}";
    }

    string IRemoteServices.GetRemoteFallbackURL(string fileName)
    {
        return $"{_fallbackHostServer}/{fileName}";
    }
}

public enum PackageType
{
    DefaultPackage,
    RawPackage,
    ShortKey
}