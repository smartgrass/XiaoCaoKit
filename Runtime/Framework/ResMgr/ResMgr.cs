using UnityEngine;
using XiaoCao;
using YooAsset;


public class ResMgr : Singleton<ResMgr> {
    public static void LoadExample(string path)
    {
        //加载 1
        GameObject go = GameObject.Instantiate(LoadPrefab(path));
        //加载 2
        go = PoolMgr.Inst.Get(path);
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
        return task.AssetObject ;
    }
    //对于只需要加载一遍的
    public static Object LoadCache(string path)
    {
        var task = Loader.LoadAssetSync(path);
        return task.AssetObject ;
    }


    #region Init
    public const string PACKAGENAME = "DefaultPackage";

    public const string RESDIR = "Assets/_Res";

    private ResourcePackage package;
    public static ResourcePackage Loader => ResMgr.Inst.package;

    public void InitYooAsset()
    {
        // 初始化资源系统
        YooAssets.Initialize();
        // 创建默认的资源包
        package = YooAssets.CreatePackage(PACKAGENAME);
        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(package);

    }
    //需要等待
    public InitializationOperation InitPackage()
    {
        // 创建默认的资源包
        string packageName = PACKAGENAME;
        var package = YooAssets.TryGetPackage(packageName);
        if (package == null)
        {
            package = YooAssets.CreatePackage(packageName);
            YooAssets.SetDefaultPackage(package);
        }

        this.package = package;
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
            initializationOperation = package.InitializeAsync(createParameters);
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
            initializationOperation = package.InitializeAsync(createParameters);
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