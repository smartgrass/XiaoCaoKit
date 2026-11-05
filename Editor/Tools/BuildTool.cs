using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XiaoCao;
using XiaoCaoEditor;
using YooAsset.Editor;

namespace XiaoCaoEditor
{
    public static class BuildTool
    {
        static string PathKey = "EditorBuildDir";

        /// <summary>Unity 未公开的"清场景 dirty"接口, 首次使用时反射获取</summary>
        private static MethodInfo _clearSceneDirtiness;

        [MenuItem(XCEditorTools.BuildAll)]
        public static void BuildAll()
        {
            StartBuld(true, true, EditorUserBuildSettings.activeBuildTarget);
        }

        /// <summary>
        /// 打包前处理未保存的场景:直接丢弃未保存改动,不弹对话框也不写磁盘。
        /// 打包读取的是磁盘上的场景文件,内存里的改动本来就不会进包,所以这里只需清掉 dirty 标记。
        /// </summary>
        public static void CheckSaveScene()
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isDirty)
                {
                    continue;
                }

                ClearDirty(scene);
                Debug.LogWarning($"--- 场景 {scene.name} 有未保存改动,打包前已丢弃(未写入磁盘)");
            }
        }

        /// <summary>
        /// 清掉场景的 dirty 标记, 效果等于放弃该场景的未保存改动(内存改动不写盘)。
        /// Unity 只公开了 MarkSceneDirty, 清标记的 ClearSceneDirtiness 是 internal, 这里反射调用。
        /// </summary>
        private static void ClearDirty(Scene scene)
        {
            if (_clearSceneDirtiness == null)
            {
                _clearSceneDirtiness = typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness",
                    BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(Scene) }, null);
            }

            _clearSceneDirtiness?.Invoke(null, new object[] { scene });
        }

        [MenuItem(XCEditorTools.BuildYooAsset)]
        public static bool BuildYooAseets()
        {
            var results = YooAssetBuildHelper.BuildYooAseets();
            var isSuccess = YooAssetBuildHelper.IsAllSucced(results);
            Debug.Log($"--- BuildYooAseets {isSuccess}");
            return isSuccess;
        }

        /// <summary>
        /// 收集主资源包的着色器变体。
        /// </summary>
        [MenuItem(XCEditorTools.CollectShaderVariants)]
        public static void CollectShaderVariants()
        {
            CheckSaveScene();

            string packageName = GetShaderVariantCollectPackageName();
            if (string.IsNullOrEmpty(packageName))
            {
                Debug.LogError("--- 未找到可收集的YooAsset包");
                return;
            }
            //
            // string savePath = GetShaderVariantSavePath(packageName);
            // ShaderVariantCollectorSetting.SetFileSavePath(packageName, savePath);
            // int processCapacity = ShaderVariantCollectorSetting.GeProcessCapacity(packageName);
            // Debug.Log($"--- CollectShaderVariants package:{packageName} savePath:{savePath}");
            // ShaderVariantCollector.Run(savePath, packageName, Mathf.Max(1, processCapacity), () =>
            // {
            //     Debug.Log($"--- CollectShaderVariants success package:{packageName} savePath:{savePath}");
            // });
        }

        public static void ProjectBuild()
        {
            CIBuildHelper.ProjectBuildExecute(EditorUserBuildSettings.activeBuildTarget);
        }

        /// <summary>
        /// 清理上一次 PC 打包残留在输出目录里的 StreamingAssets。
        /// YooAsset 的 bundle 文件名取自内容哈希, 资源一改文件名就变; 而 Unity 复制 StreamingAssets 时
        /// 只覆盖不删除, 于是 Build/ActGame_Data/StreamingAssets 会按构建次数累加(实测叠了 3 次共 671MB)。
        /// 这里在 BuildPlayer 之前整个删掉, 让 Unity 重新完整复制一份。
        /// 安卓每次生成新 apk, 不存在这个问题, 不调用。
        /// </summary>
        [MenuItem(XCEditorTools.ClearWindowBuildResidual)]
        public static void ClearStreamingAssets()
        {
            string streamingAssetsDir = GetWindowBuildStreamingAssetsDir();
            if (!Directory.Exists(streamingAssetsDir))
            {
                Debug.Log($"--- 清理打包残留: {streamingAssetsDir} 不存在, 跳过");
                return;
            }

            long size = GetDirSize(streamingAssetsDir);
            Directory.Delete(streamingAssetsDir, true);
            Debug.Log($"--- 清理打包残留: 已删除 {streamingAssetsDir} ({size / 1024f / 1024f:F1} MB)");
        }

        /// <summary>PC 输出目录里的 StreamingAssets 路径, 由打包 exe 路径推导(同名 _Data 目录)</summary>
        private static string GetWindowBuildStreamingAssetsDir()
        {
            string exePath = Path.Combine(PathTool.GetProjectPath(), CIBuildHelper.WindowBuildExePath);
            string dataDir = Path.GetFileNameWithoutExtension(exePath) + "_Data";
            return Path.Combine(Path.GetDirectoryName(exePath), dataDir, "StreamingAssets");
        }

        /// <summary>统计目录占用字节数, 仅用于清理日志</summary>
        private static long GetDirSize(string dir)
        {
            long size = 0;
            foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                size += new FileInfo(file).Length;
            }

            return size;
        }

        [MenuItem(XCEditorTools.CopyExtraResToWin)]
        public static void CopyExtraResToWin()
        {
            //win->build下面 ->buildAfter
            //Android ->streamAsset下 ->buildBefore
            string buildDir = EditorPrefs.GetString(PathKey);
            if (string.IsNullOrEmpty(buildDir))
            {
                buildDir = PathTool.GetProjectPath() + "/Build";
            }

            CopyDirToWindowBuild(buildDir);
        }


        [PostProcessBuild(1)]
        public static void AfterBuild(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Build Success  输出平台: " + target + "  输出路径: " + pathToBuiltProject);
            var buildDir = pathToBuiltProject.Substring(0, pathToBuiltProject.LastIndexOf("/"));
            Debug.Log("buildPath:" + buildDir);

            EditorPrefs.SetString(PathKey, buildDir);
            if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
            {
                CopyDirToWindowBuild(buildDir);
            }
        }

        [MenuItem(XCEditorTools.CopyZipToAndroidBuild)]
        public static void CopyZipToAndroidBuild()
        {
            //旧的存在则删掉
            DeleteStreamingAssetsExtraResZip();
            string zipPath = Path.Combine(Application.streamingAssetsPath, "ExtraRes.zip");
            string sourceDir = $"{PathTool.GetProjectPath()}/ExtraRes/{BuildTarget.Android}";
            string sourceDir2 = $"{PathTool.GetProjectPath()}/ExtraRes/GameConfig";
            ZipHelper.CompressFolders(new[] { sourceDir, sourceDir2 }, zipPath);
            AssetDatabase.Refresh();
            Debug.Log($"-- CopyZip to streamingAssetsPath");
        }

        public static void DeleteStreamingAssetsExtraResZip()
        {
            string zipPath = Path.Combine(Application.streamingAssetsPath, "ExtraRes.zip");
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
                Debug.Log($"-- 删除 streamingAssetsPath/ExtraRes.zip");
            }
        }

        private static void CopyDirToWindowBuild(string buildDir)
        {
            string tgtDir = XCPathConfig.GetWindowBuildResDir() + "/ExtraRes";
            if (!Directory.Exists(tgtDir))
            {
                Directory.CreateDirectory(tgtDir);
            }

            string sourceDir = XCPathConfig.GetExtraPackageDir();

            //检测打包文件是否正确
            var reportFilePathList = FileTool.FindFiles(sourceDir, "BuildReport*.json");
            foreach (var _reportFilePath in reportFilePathList)
            {
                string jsonData = FileTool.ReadFileString(_reportFilePath);
                var _buildReport = BuildReport.Deserialize(jsonData);
                if (_buildReport.Summary.BuildTarget != BuildTarget.StandaloneWindows64)
                {
                    Debug.LogError(
                        $"returen : {_buildReport.Summary.BuildPackageName} is {_buildReport.Summary.BuildTarget}");
                    return;
                }
            }

            FileTool.CopyFolder(XCPathConfig.GetGameConfigDir(), tgtDir);
            FileTool.CopyFolder(XCPathConfig.GetExtraPackageDir(), tgtDir);
        }

        /// <summary>
        /// 优先收集主资源包，未配置时回退到第一个可构建包。
        /// </summary>
        private static string GetShaderVariantCollectPackageName()
        {
            var packageNames = YooAssetBuildHelper.GetYooAssetBuildPackageNames();
            if (packageNames.Count == 0)
            {
                return string.Empty;
            }

            if (packageNames.Contains(ResMgr.DefaultPackage))
            {
                return ResMgr.DefaultPackage;
            }

            return packageNames[0];
        }
        
        public static void StartBuld(bool IsBuildYooAseet, bool IsBuildPackage, BuildTarget buildTarget)
        {
            BuildTool.CheckSaveScene();

            bool isAndriod = buildTarget == BuildTarget.Android;

            CIBuildHelper.SwitchPlatform(buildTarget);

            if (IsBuildYooAseet)
            {
                var results = YooAssetBuildHelper.BuildYooAseets();

                if (!YooAssetBuildHelper.IsAllSucced(results))
                {
                    Debug.LogError($"--- BuildYooAseet fail");
                    return;
                }
            }

            if (isAndriod)
            {
                BuildTool.CopyZipToAndroidBuild();
            }
            else
            {
                BuildTool.DeleteStreamingAssetsExtraResZip();
            }

            ConfigMgr.StaticSettingSo.buildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");


            if (IsBuildPackage)
            {
                // 放在 BuildPlayer 之前: 上面 YooAsset 构建失败会提前 return, 不会误清上一次可用的包
                if (!isAndriod)
                {
                    BuildTool.ClearStreamingAssets();
                }

                BuildTool.ProjectBuild();
            }
        }
    }
}


public static class CIBuild
{
    public static void Build()
    {
        Debug.Log($"--- CIBuild BuildAll ");
        BuildTool.BuildAll();
    }
}
