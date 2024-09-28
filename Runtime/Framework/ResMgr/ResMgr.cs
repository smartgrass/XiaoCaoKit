using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using XiaoCao;
using YooAsset;


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

    public static ResourcePackage Loader;
    public static ResourcePackage ExtraLoader;
    public static ResourcePackage RawLoader;

    public static bool hasExtraPackage;

    public static GameObject LoadInstan(string path, PackageType type = PackageType.DefaultPackage)
    {
        return GameObject.Instantiate(LoadPrefab(path, type));
    }

    //只加载, 没有实例化
    public static GameObject LoadPrefab(string path, PackageType type = PackageType.DefaultPackage)
    {
        if (type == PackageType.DefaultPackage || !hasExtraPackage)
        {
            var task = Loader.LoadAssetSync<GameObject>(path);
            return task.AssetObject as GameObject;
        }
        else
        {
            if (ExtraLoader.CheckLocationValid(path))
            {
                Debug.Log($"--- ExtraLoader {path}");
                return ExtraLoader.LoadAssetSync<GameObject>(path).AssetObject as GameObject;
            }
            else
            {
                Debug.LogWarning($"--- no Extra FailBack to Default {path}");
            return LoadPrefab(path, PackageType.DefaultPackage);
            }
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
        var handle = RawLoader.LoadRawFileSync(path);
        byte[] fileData = handle.GetRawFileData();
        return fileData;
    }


    #region Init
    public static void InitYooAsset()
    {

        // 初始化资源系统
        YooAssets.Initialize();
        // 创建默认的资源包
        Loader = YooAssets.CreatePackage(DefaultPackage);
        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(Loader);
    }

    public static async Task InitExtraPackage()
    {
        //EXTRA包的作用: 加载工程外资源
        //编辑器模拟: 不使用,没意义
        //离线编辑器: 加载 streamingAssetsPath 判断文件是否存在
        //离线Exe: 加载指定位置资源,并且需要判断文件是否存在
        string packageName = ExtraPackage;
        ResourcePackage package = GetOrCreatPackage(packageName);
        InitializationOperation initOperation = null;
        EPlayMode playMode = GetEPlayMode();
        ExtraLoader = package;

        string defaultHostServer = GetExtraPackageUrl(out bool hasManifest);
        string fallbackHostServer = defaultHostServer;
        hasExtraPackage = hasManifest;

        playMode = EPlayMode.HostPlayMode;
        //var initParameters = new HostPlayModeParameters();
        //FileSystemParameters.CreateDefaultWebFileSystemParameters()
        //initParameters.DeliveryFileSystemParameters = new GameQueryServices();
        //initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);

        IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        var cacheFileSystem = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
        //var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
        var initParameters = new HostPlayModeParameters();
        initParameters.BuildinFileSystemParameters = cacheFileSystem;
        initParameters.CacheFileSystemParameters = cacheFileSystem;


        initOperation = package.InitializeAsync(initParameters);

        Debug.Log($"--- initializationOperation task");
        await initOperation.Task;

        if (initOperation.Status == EOperationStatus.Succeed)
            Debug.Log("资源包初始化成功！");
        else
            Debug.LogError($"资源包初始化失败：{initOperation.Error}");
        await Task.Yield();



        var operation1 = package.RequestPackageVersionAsync();
        await operation1;
        var operation2 = package.UpdatePackageManifestAsync(operation1.PackageVersion);
        await operation2;

        ExtraLoader.GetAssetInfos("").LogListStr("--");
    }



    public static async Task InitRawPackage()
    {
        EPlayMode playMode = GetEPlayMode();

        string packageName = RawPackage;

        ResourcePackage package = GetOrCreatPackage(packageName);

        RawLoader = package;

        InitializationOperation initializationOperation = null;

#if UNITY_EDITOR
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            //注意：如果是原生文件系统选择EDefaultBuildPipeline.RawFileBuildPipeline
            var buildPipeline = EDefaultBuildPipeline.RawFileBuildPipeline;
            var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(buildPipeline, RawPackage);
            var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult);
            var initParameters = new EditorSimulateModeParameters();
            initParameters.EditorFileSystemParameters = editorFileSystem;
            initializationOperation = package.InitializeAsync(initParameters);
        }
#endif

        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var initParameters = new OfflinePlayModeParameters();
            initParameters.BuildinFileSystemParameters = buildinFileSystem;
            initializationOperation = package.InitializeAsync(initParameters);
        }
        await initializationOperation.Task;

        var operation1 = package.RequestPackageVersionAsync();
        await operation1;
        var operation2 = package.UpdatePackageManifestAsync(operation1.PackageVersion);
        await operation2;

    }

    //需要等待
    public static async Task InitDefaultPackage()
    {
        string packageName = DefaultPackage;
        ResourcePackage package = GetOrCreatPackage(packageName);
        InitializationOperation initializationOperation = null;
        EPlayMode playMode = GetEPlayMode();
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
            initializationOperation = package.InitializeAsync(initParameters);
            Debug.Log($"--- EditorSimulateMode");
        }
#endif
        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var par = new OfflinePlayModeParameters();
            initializationOperation = package.InitializeAsync(par);
        }

        // 联机运行模式
        if (playMode == EPlayMode.HostPlayMode)
        {
            //string defaultHostServer = "http://127.0.0.1/CDN/Android/v1.0";
            //string fallbackHostServer = "http://127.0.0.1/CDN/Android/v1.0";
            //var initParameters = new HostPlayModeParameters();
            //initParameters.BuildinQueryServices = new GameQueryServices();
            //initParameters.DecryptionServices = new FileOffsetDecryption();
            //initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer
            //initializationOperation =package.InitializeAsync(initParameters);
            Debuger.LogWarning($"HostPlayMode 无");
        }
        await initializationOperation;

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

    private static string GetExtraPackageUrl(out bool hasManifest)
    {
        string packName = ExtraPackage;
        if (Application.isEditor)
        {
            string path = $"{PathTool.GetProjectPath()}/Build/Res/{packName}/";

            hasManifest = HasManifest(path, packName);

            Debug.Log($"--- hasManifest {hasManifest}");

            return $"file://{path}";
        }
        else
        {
            string path = Application.dataPath + "/Bundles/StandaloneWindows64/ExtraPackage/v1";

            hasManifest = HasManifest(path, packName);

            return $"file://{path}";
        }
    }

    private static bool HasManifest(string dir, string packName)
    {
        string fileName = $"PackageManifest_{packName}.version";
        string filePath = Path.Combine(dir, fileName);

        bool ret = FileTool.IsFileExist(filePath);

        Debug.Log($"has {ret} {filePath}");
        return ret;
    }


    private static EPlayMode GetEPlayMode()
    {
        EPlayMode playMode = EPlayMode.OfflinePlayMode;
#if UNITY_EDITOR
        playMode = (EPlayMode)UnityEditor.EditorPrefs.GetInt("EditorResourceMode");
#else
        playMode = EPlayMode.OfflinePlayMode;
#endif

        return playMode;
    }

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
    ExtraPackage
}