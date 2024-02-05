using System.IO;
using UnityEditor;
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

    public const string PACKAGENAME = "DefaultPackage";
    public const string PACKAGENAME_RAW = "RawPackage";
    public const string PACKAGENAME_EXTRA = "ExtraPackage"; //用于Mod

    public const string RESDIR = "Assets/_Res";

    public static ResourcePackage Loader;
    public static ResourcePackage ExtraLoader;
    public static ResourcePackage RawLoader;

    public static GameObject LoadInstan(string path, PackageType type = PackageType.DefaultPackage)
    {
        return GameObject.Instantiate(LoadPrefab(path, type));
    }

    //只加载, 没有实例化
    public static GameObject LoadPrefab(string path, PackageType type = PackageType.DefaultPackage)
    {
        if (type == PackageType.DefaultPackage)
        {
            var task = Loader.LoadAssetSync<GameObject>(path);
            return task.AssetObject as GameObject;
        }
        else
        {
            foreach (var item in ExtraLoader.GetAssetInfos(""))
            {
                Debug.Log($"--- {item}");
            }

            //Extra
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
        Loader = YooAssets.CreatePackage(PACKAGENAME);
        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(Loader);
    }

    public static InitializationOperation InitExtraPackage()
    {
        var tempPack = YooAssets.TryGetPackage(PACKAGENAME_EXTRA);
        if (tempPack == null)
        {
            tempPack = YooAssets.CreatePackage(PACKAGENAME_EXTRA);
        }
        ExtraLoader = tempPack;
        Debug.Log($"--- {tempPack == null} {tempPack.PackageName} ");

        InitializationOperation initializationOperation = null;

        EPlayMode playMode = GetEPlayMode();

#if UNITY_EDITOR
        //编辑器模式使用。
        //Debuger.Warn($"编辑器模式使用:{playMode}");
        //// 编辑器下的模拟模式
        //if (playMode == EPlayMode.EditorSimulateMode)
        //{
        //    var par = new EditorSimulateModeParameters();
        //    par.SimulateManifestFilePath =
        //        EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, PACKAGENAME_EXTRA);
        //    initializationOperation = tempPack.InitializeAsync(par);
        //}
        //return initializationOperation;
#endif

        string defaultHostServer = GetExtraPackageUrl();
        string fallbackHostServer = defaultHostServer;

        Debug.LogError($"--- {HasManifest(defaultHostServer, PACKAGENAME_EXTRA)}");

        var initParameters = new HostPlayModeParameters();
        //内置文件查询
        initParameters.BuildinQueryServices = new GameQueryServices();
        //解密不需要
        //initParameters.DecryptionServices = new FileOffsetDecryption();
        initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);


       initializationOperation = tempPack.InitializeAsync(initParameters);

        //需求是 检查Manifest是否存在,不存在则不执行
        string mainifeatPath = "PackageManifest_ExtraPackage";
        //initializationOperation.PackageVersion

        return initializationOperation;
    }

    private static bool HasManifest(string dir,string packName)
    {
        string fileName = $"PackageManifest_{packName}.version";
        string filePath = Path.Combine(dir, fileName);
        filePath = filePath.RemoveHead("file://");
        Debug.Log($"--- filePath {filePath} {FileTool.IsFileExist(filePath)}");
        return FileTool.IsFileExist(filePath);
    }

    private static string GetExtraPackageUrl()
    {
        if (Application.isEditor)
        {
            return "file://" + Application.streamingAssetsPath + "/yoo/ExtraPackage/";
        }
        else
        {
            //设置扩展资源路径
            return "file://" + Application.dataPath + "/Bundles/StandaloneWindows64/ExtraPackage/v1";
        }
    }

    public static InitializationOperation InitRawPackage()
    {
        EPlayMode playMode = GetEPlayMode();

        RawLoader = YooAssets.CreatePackage(PACKAGENAME_RAW);
        InitializationOperation initializationOperation = null;

#if UNITY_EDITOR
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var rawPar = new EditorSimulateModeParameters();
            rawPar.SimulateManifestFilePath =
                EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.RawFileBuildPipeline, PACKAGENAME_RAW);
            initializationOperation = RawLoader.InitializeAsync(rawPar);
        }
#endif

        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var par = new OfflinePlayModeParameters();
            initializationOperation = RawLoader.InitializeAsync(par);
        }


        return initializationOperation;
    }

    //需要等待
    public static InitializationOperation InitPackage()
    {
        // 创建默认的资源包
        var tempPack = YooAssets.TryGetPackage(PACKAGENAME);
        if (tempPack == null)
        {
            tempPack = YooAssets.CreatePackage(PACKAGENAME);
            YooAssets.SetDefaultPackage(tempPack);
        }

        Loader = tempPack;


        InitializationOperation initializationOperation = null;

        EPlayMode playMode = GetEPlayMode();
#if UNITY_EDITOR
        //编辑器模式使用。
        Debuger.Warn($"编辑器模式使用:{playMode}");
        // 编辑器下的模拟模式
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var par = new EditorSimulateModeParameters();
            par.SimulateManifestFilePath =
                EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, PACKAGENAME);
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
            Debuger.Warn($"HostPlayMode 无");
        }

        return initializationOperation;
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