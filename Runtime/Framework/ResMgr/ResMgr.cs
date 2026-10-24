using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using XiaoCao;
using YooAsset;
using Debug = UnityEngine.Debug;
using EPlayMode = YooAsset.EPlayMode;

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
    public static bool IsLoadBaseFinish;
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
            if (prefab != null)
            {
                return GameObject.Instantiate(prefab);
            }

            // Mod 资源缺失时回退默认外观, 避免整局加载中断
            Debug.LogWarning($"--- shortKey {shortKey} 加载失败, 回退默认外观 {failBackPath}");
        }

        return ResMgr.LoadInstan(failBackPath, PackageType.DefaultPackage);
    }


    //只加载, 没有实例化
    public static GameObject LoadPrefab(string path, PackageType type = PackageType.DefaultPackage)
    {
        var task = Loader.LoadAssetSync<GameObject>(path);
        return task.AssetObject as GameObject;

        //if (ExtraLoader.IsLocationValid(path))
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

        if (Loader.IsLocationValid(path))
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

    /// <summary>
    /// 加载图集里的子资源, 例: Crystal_B.png 中的子图 Crystal_B_1, 找不到返回null
    /// </summary>
    public static T LoadSubAseet<T>(string path, string subAssetName) where T : Object
    {
        if (!Loader.IsLocationValid(path))
        {
            Debug.LogWarning($"--- no Asset {path}");
            return null;
        }

        var handle = Loader.LoadSubAssetsSync<T>(path);
        return handle.GetSubAssetObject<T>(subAssetName);
    }

    //只加载, 没有实例化
    public static Object LoadAseet(string path)
    {
        if (Loader.IsLocationValid(path))
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
        if (!RawLoader.IsLocationValid(path))
        {
            return null;
        }

        // v3: 原生文件以 RawFileObject 形式加载, 不再提供 GetRawFileData()
        var handle = RawLoader.LoadAssetSync<RawFileObject>(path);
        var rawFile = handle.AssetObject as RawFileObject;
        return rawFile != null ? rawFile.GetBytes() : null;
    }

    #region Init

    public static async UniTask InitYooAssetAll()
    {
        ResMgr.InitYooAsset();
        ShortKeyDic.Clear();
        var task1 = ResMgr.InitDefaultPackage();
        var task2 = ResMgr.InitRawPackage();
        var task3 = ResMgr.InitExtraPackage();
        //使用并行任务,相比同步快个300ms
        //task3大概需要4s
        await UniTask.WhenAll(task1, task2);
        IsLoadBaseFinish = true;
        await UniTask.WhenAll(task3);
        IsLoadFinish = true;
    }

    public static void InitYooAsset()
    {
        // 初始化资源系统
        YooAssets.Initialize();
        // 创建默认的资源包（v3 取消 SetDefaultPackage, 加载一律走 package 实例）
        Loader = GetOrCreatPackage(DefaultPackage);
    }

    public static async UniTask InitDefaultPackage()
    {
        string packageName = DefaultPackage;
        Debug.Log($"--- InitPackage {packageName}");
        ResourcePackage package = GetOrCreatPackage(packageName);
        InitializePackageOperation initOperation = null;
        EPlayMode playMode = DebugSetting.GetEPlayMode();
        Loader = package;

#if UNITY_EDITOR
        //编辑器模式使用。
        Debuger.LogWarning($"编辑器模式使用:{playMode}");
        // 编辑器下的模拟模式：v3 由 EditorSimulateBuildInvoker 生成模拟清单，产物根目录交给编辑器文件系统
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var simulateBuildResult = EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualAssetBundle);
            var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult.PackageRootDirectory);
            var options = new EditorSimulateModeOptions();
            options.EditorFileSystemParameters = editorFileSystem;
            initOperation = package.InitializePackageAsync(options);
            Debug.Log($"--- EditorSimulateMode");
        }
#endif
        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var options = new OfflinePlayModeOptions();
            options.BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
            initOperation = package.InitializePackageAsync(options);
        }

        await UpdatePackage(package, initOperation);
    }


    public static async UniTask InitExtraPackage()
    {
        foreach (IniSection section in ConfigMgr.Inst.MainCfg.SectionList)
        {
            if (section.SectionName.StartsWith("Mod"))
            {
                var packageName = section.SectionName;
                ResourcePackage package = GetOrCreatPackage(packageName);

                string defaultHostServer = GetExtraPackageUrl(packageName, out bool hasManifest);
                if (!hasManifest)
                {
                    Debug.LogWarning($"--- continue No Package {packageName} {defaultHostServer}");
                    continue;
                }

                Debug.Log($"--- InitPackage {packageName} {defaultHostServer}");

                // ExtraPackage 无论单机还是模拟都使用远端, 区别只在远端地址
                IRemoteService remoteService = new RemoteServices(defaultHostServer, defaultHostServer);
                var options = new HostPlayModeOptions();
                // Mod 包只在远端(本地 file://)提供，不挂内置文件系统，否则会去 StreamingAssets 找 BuiltinCatalog
                options.CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService);
                InitializePackageOperation initOperation = package.InitializePackageAsync(options);

                await UpdatePackage(package, initOperation);

                // Mod 包是纯远端包(本地 file://)，而项目多数资源走同步加载，
                // 同步加载不会触发下载，所以初始化后先把内容全部拉进沙箱缓存
                var downloader = package.CreateResourceDownloader(new ResourceDownloaderOptions(8, 3));
                downloader.StartDownload();
                await downloader;
                if (downloader.Status != EOperationStatus.Succeeded)
                {
                    Debug.LogError($"--- 下载 Mod 包失败 {packageName}: {downloader.Error}");
                    continue;
                }

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

        InitializePackageOperation initOperation = null;

#if UNITY_EDITOR
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            // 模拟构建只接受 Virtual 类型，原生文件包对应 VirtualRawBundle
            var simulateBuildResult = EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualRawBundle);
            var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult.PackageRootDirectory);
            var options = new EditorSimulateModeOptions();
            options.EditorFileSystemParameters = editorFileSystem;
            initOperation = package.InitializePackageAsync(options);
        }
#endif

        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var options = new OfflinePlayModeOptions();
            options.BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
            initOperation = package.InitializePackageAsync(options);
        }

        await UpdatePackage(package, initOperation);
    }

    private static async Task UpdatePackage(ResourcePackage package, InitializePackageOperation initOperation)
    {
        if (initOperation == null)
        {
            Debug.LogError($"--- InitPackage 失败: {package.PackageName} 未匹配到运行模式 {DebugSetting.GetEPlayMode()}");
            return;
        }

        await initOperation;

        var versionOperation = package.RequestPackageVersionAsync();
        await versionOperation;
        // 注意：v3 的 GetPackageVersion() 要求已激活 manifest，这里只能取操作返回的版本号
        string packageVersion = versionOperation.PackageVersion;

        var manifestOperation = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(packageVersion, 60));
        await manifestOperation;

        Debug.Log($"--- InitPackageEnd {package.PackageName} {packageVersion}");
    }

    private static ResourcePackage GetOrCreatPackage(string packageName)
    {
        if (YooAssets.TryGetPackage(packageName, out ResourcePackage package))
        {
            return package;
        }

        return YooAssets.CreatePackage(packageName);
    }

    private static string GetExtraPackageUrl(string packName, out bool hasManifest)
    {
        string path = $"{XCPathConfig.GetExtraPackageDir()}/{packName}/";

        hasManifest = HasManifest(path, packName);

        // file:// 地址必须用正斜杠，Windows 的绝对路径里带反斜杠会拼出非法 URL
        return $"file://{path.Replace('\\', '/')}";
    }

    private static bool HasManifest(string dir, string packName)
    {
        // v3 的版本文件名规则: {PackageName}.version（配置了 PackageFilePrefix 时带前缀）
        string fileName = YooAssetConfiguration.GetPackageVersionFileName(packName);
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
public class RemoteServices : IRemoteService
{
    private readonly string _defaultHostServer;
    private readonly string _fallbackHostServer;

    public RemoteServices(string defaultHostServer, string fallbackHostServer)
    {
        _defaultHostServer = defaultHostServer;
        _fallbackHostServer = fallbackHostServer;
    }

    /// <summary>
    /// 按优先级返回候选地址：主地址在前，备用地址在后
    /// </summary>
    public IReadOnlyList<string> GetRemoteUrls(string fileName)
    {
        return new List<string>
        {
            $"{_defaultHostServer}/{fileName}",
            $"{_fallbackHostServer}/{fileName}",
        };
    }
}

public enum PackageType
{
    DefaultPackage,
    RawPackage,
    ShortKey
}
