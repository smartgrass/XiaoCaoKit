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
    }

    public static GameObject LoadInstan(string path)
    {
        return GameObject.Instantiate(LoadPrefab(path));
    }

    //只加载, 没有实例化
    public static GameObject LoadPrefab(string path)
    {
        var task = Loader.LoadAssetSync<GameObject>(path);
        return task.AssetObject as GameObject;
    }
    //只加载, 没有实例化
    public static Object LoadAseet(string path)
    {
        var task = Loader.LoadAssetSync(path);
        return task.AssetObject;
    }

    public static byte[] LoadByte(string path)
    {
        Debug.Log($"---  {path}");
        var handle = Loader.LoadRawFileSync(path);
        byte[] fileData = handle.GetRawFileData();
        return fileData;
    }


    #region Init
    public const string PACKAGENAME = "DefaultPackage";

    public const string RESDIR = "Assets/_Res";

    public static ResourcePackage Loader;

    public static void InitYooAsset()
    {
        // 初始化资源系统
        YooAssets.Initialize();
        // 创建默认的资源包
        Loader = YooAssets.CreatePackage(PACKAGENAME);
        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(Loader);

    }
    //需要等待
    public static InitializationOperation InitPackage()
    {
        // 创建默认的资源包
        string packageName = PACKAGENAME;
        var tempPack = YooAssets.TryGetPackage(packageName);
        if (tempPack == null)
        {
            tempPack = YooAssets.CreatePackage(packageName);
            YooAssets.SetDefaultPackage(tempPack);
        }
        Loader = tempPack;

        InitializationOperation initializationOperation = null;

#if UNITY_EDITOR
        //编辑器模式使用。
        EPlayMode playMode = (EPlayMode)UnityEditor.EditorPrefs.GetInt("EditorResourceMode");
        Debuger.Warn($"编辑器模式使用:{playMode}");

        // 编辑器下的模拟模式
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var createParameters = new EditorSimulateModeParameters();
            createParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, packageName);
            initializationOperation = tempPack.InitializeAsync(createParameters);
        }
#else
        //运行时使用。
        EPlayMode playMode = EPlayMode.OfflinePlayMode;
        Debuger.Warn($"playMode:{playMode}");
#endif

        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var createParameters = new OfflinePlayModeParameters();
            initializationOperation = tempPack.InitializeAsync(createParameters);
        }

        // 联机运行模式
        if (playMode == EPlayMode.HostPlayMode)
        {
            Debuger.Warn($"HostPlayMode 无");
        }
        return initializationOperation;
    }

    #endregion
    /*
    https://www.yooasset.com/docs/guide-runtime/CodeTutorial3
    LoadSceneAsync() 异步加载场景
    LoadAssetSync() 同步加载资源对象
    LoadAssetAsync() 异步加载资源对象
    LoadSubAssetsSync() 同步加载子资源对象
    LoadSubAssetsAsync() 异步加载子资源对象
    LoadAllAssetsSync() 同步加载资源包内所有资源对象
    LoadAllAssetsAsync() 异步加载资源包内所有资源对象
    LoadRawFileSync() 同步获取原生文件
    LoadRawFileAsync() 异步获取原生文件
    加载示例
    var handle = ResMgr.Loader.LoadAssetSync<GameObject>(eName);
    prefab = handle.AssetObject as GameObject;
*/
}