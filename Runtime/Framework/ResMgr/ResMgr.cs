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

    public const string PACKAGENAME_DEFAULT = "DefaultPackage";
    public const string PACKAGENAME_RAW = "RawPackage";
    public const string PACKAGENAME_EXTRA = "ExtraPackage"; //用于Mod

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
        var task = Loader.LoadAssetSync(path);
        return task.AssetObject;
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
        Loader = YooAssets.CreatePackage(PACKAGENAME_DEFAULT);
        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(Loader);
    }

    public static async Task InitExtraPackage()
    {
        //EXTRA包的作用: 加载工程外资源
        //编辑器模拟: 不使用,没意义
        //离线编辑器: 加载 streamingAssetsPath 判断文件是否存在
        //离线Exe: 加载指定位置资源,并且需要判断文件是否存在
        string packageName = PACKAGENAME_EXTRA;
        ResourcePackage tempPack = GetOrCreatPackage(packageName);
        InitializationOperation initializationOperation = null;
        EPlayMode playMode = GetEPlayMode();
        ExtraLoader = tempPack;

        string defaultHostServer = GetExtraPackageUrl(packageName, out bool hasManifest);
        string fallbackHostServer = defaultHostServer;
        hasExtraPackage = hasManifest;

        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var initParameters = new HostPlayModeParameters();
            initParameters.BuildinQueryServices = new GameQueryServices();
            initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            initializationOperation = tempPack.InitializeAsync(initParameters);
            await initializationOperation.Task;
        }
        await Task.Yield();
    }



    public static Task InitRawPackage()
    {
        EPlayMode playMode = GetEPlayMode();

        string packageName = PACKAGENAME_RAW;

        ResourcePackage tempPack = GetOrCreatPackage(packageName);

        RawLoader = tempPack;

        InitializationOperation initializationOperation = null;

#if UNITY_EDITOR
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var rawPar = new EditorSimulateModeParameters();
            rawPar.SimulateManifestFilePath =
                EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.RawFileBuildPipeline, PACKAGENAME_RAW);
            initializationOperation = tempPack.InitializeAsync(rawPar);
        }
#endif

        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var par = new OfflinePlayModeParameters();
            initializationOperation = tempPack.InitializeAsync(par);
        }


        return initializationOperation.Task;
    }

    //需要等待
    public static Task InitDefaultPackage()
    {
        string packageName = PACKAGENAME_DEFAULT;
        ResourcePackage tempPack = GetOrCreatPackage(packageName);
        InitializationOperation initializationOperation = null;
        EPlayMode playMode = GetEPlayMode();
        Loader = tempPack;

#if UNITY_EDITOR
        //编辑器模式使用。
        Debuger.LogWarning($"编辑器模式使用:{playMode}");
        // 编辑器下的模拟模式
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var par = new EditorSimulateModeParameters();
            par.SimulateManifestFilePath =
                EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, packageName);
            initializationOperation = tempPack.InitializeAsync(par);
        }
#endif
        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var par = new OfflinePlayModeParameters();
            initializationOperation = tempPack.InitializeAsync(par);
        }

        // 联机运行模式
        if (playMode == EPlayMode.HostPlayMode)
        {
            string defaultHostServer = "http://127.0.0.1/CDN/Android/v1.0";
            string fallbackHostServer = "http://127.0.0.1/CDN/Android/v1.0";
            var initParameters = new HostPlayModeParameters();
            //initParameters.BuildinQueryServices = new GameQueryServices();
            //initParameters.DecryptionServices = new FileOffsetDecryption();
            //initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer
            //initializationOperation =package.InitializeAsync(initParameters);
            Debuger.LogWarning($"HostPlayMode 无");
        }

        return initializationOperation.Task;
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
        if (Application.isEditor)
        {
            string path = $" {Application.streamingAssetsPath}/yoo/{packName}/";

            hasManifest = HasManifest(path, packName);

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