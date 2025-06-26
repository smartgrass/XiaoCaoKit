using System;
using System.Collections.Generic;
using System.IO;
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

        [MenuItem(XCEditorTools.BuildAll)]
        public static void BuildAll()
        {
            StartBuld(true, true, EditorUserBuildSettings.activeBuildTarget);
        }

        public static void CheckSaveScene()
        {
            if (EditorTools.HasDirtyScenes())
            {
                Scene currentScene = SceneManager.GetActiveScene();
                if (EditorUtility.DisplayDialog("Scene 未保存",
                    "是否继续?", "保存并继续",
                    "取消"))
                {
                    bool save = EditorSceneManager.SaveScene(currentScene);
                    if (!save)
                    {
                        throw new Exception("--- 保存失败");
                    }
                }
                else
                {
                    throw new Exception("--- 取消");
                }
            }
        }

        public static bool BuildYooAseets()
        {
            BuildResult result = YooAssetBuildHelper.BuildYooAseets();
            Debug.Log($"--- BuildYooAseets {result.Success}");
            return result.Success;
        }

        public static void ProjectBuild()
        {
            CIBuildHelper.ProjectBuildExecute(EditorUserBuildSettings.activeBuildTarget);
        }

        public static void ClearStreamingAssets()
        {
            Debug.Log($"--- ClearStreamingAssets");


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
            string zipPath = Path.Combine(Application.streamingAssetsPath, "ExtraRes.zip");
            string sourceDir = $"{PathTool.GetProjectPath()}/ExtraRes/{BuildTarget.Android}";
            string sourceDir2 = $"{PathTool.GetProjectPath()}/ExtraRes/GameConfig";
            ZipHelper.CompressFolders(new[] { sourceDir, sourceDir2 }, zipPath);
            AssetDatabase.Refresh();
        }

        public static void DeleteExtraResZip()
        {
            string zipPath = Path.Combine(Application.streamingAssetsPath, "ExtraRes.zip");
            File.Delete(zipPath);
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
                    Debug.LogError($"returen : {_buildReport.Summary.BuildPackageName} is {_buildReport.Summary.BuildTarget}");
                    return;
                }
            }

            FileTool.CopyFolder(XCPathConfig.GetGameConfigDir(), tgtDir);
            FileTool.CopyFolder(XCPathConfig.GetExtraPackageDir(), tgtDir);
        }

        public static void StartBuld(bool IsBuildYooAseet, bool IsBuildPackage, BuildTarget buildTarget)
        {
            BuildTool.CheckSaveScene();

            bool isAndriod = buildTarget == BuildTarget.Android;

            CIBuildHelper.SwitchPlatform(buildTarget);

            if (!isAndriod)
            {
                BuildTool.ClearStreamingAssets();
            }


            if (IsBuildYooAseet)
            {
                BuildResult result = YooAssetBuildHelper.BuildYooAseets();
                if (!result.Success)
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
                BuildTool.DeleteExtraResZip();
            }
            ConfigMgr.StaticSettingSo.buildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");


            if (IsBuildPackage)
            {
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
