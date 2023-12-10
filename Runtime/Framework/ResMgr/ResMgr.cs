using XiaoCao;
using YooAsset;

/// <summary>
///<see cref="ResMgr.Loader"/>
/// </summary>
public class ResMgr : Singleton<ResMgr> {
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
    public string PackageName = "DefaultPackage";

    private ResourcePackage package;
    public static ResourcePackage Loader => ResMgr.Inst.package;

    public void InitYooAsset()
    {
        // 初始化资源系统
        YooAssets.Initialize();
        // 创建默认的资源包
        package = YooAssets.CreatePackage(PackageName);
        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(package);

    }
    //需要等待
    public InitializationOperation InitPackage()
    {
        // 创建默认的资源包
        string packageName = PackageName;
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
}