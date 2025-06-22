using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XiaoCao;
using YooAsset.Editor;

namespace XiaoCaoEditor
{

    public static class BuildTool
    {
        static string PathKey = "EditorBuildDir";

        [MenuItem(XCEditorTools.BuildAll)]
        public static void BuildAll()
        {
            CheckSaveScene();
            BuildYooAseets();
            ProjectBuild();
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

        //[MenuItem(XCEditorTools.CopyConfigToSteamAssets)]
        public static void CopyLubanConfig()
        {
            //win->build下面 ->buildAfter
            //Android ->streamAsset下 ->buildBefore
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {

            }
            else
            {
                string buildDir = EditorPrefs.GetString("EditorBuildDir");
                if (string.IsNullOrEmpty(buildDir))
                {
                    buildDir = PathTool.GetProjectPath() + "/Build";
                }
                CopyDirToWindowBuild(buildDir);
            }
        }

        [MenuItem(XCEditorTools.CreateZip)]
        public static void CreateZip()
        {
            SaveZipTool.Create();
        }


        [PostProcessBuild(1)]
        public static void AfterBuild(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Build Success  输出平台: " + target + "  输出路径: " + pathToBuiltProject);
            var buildDir = pathToBuiltProject.Substring(0, pathToBuiltProject.LastIndexOf("/"));
            Debug.Log("buildPath:" + buildDir);

            EditorPrefs.SetString(PathKey, buildDir);
            if (target == BuildTarget.StandaloneWindows)
            {
                CopyDirToWindowBuild(buildDir);
            }

        }

        [MenuItem(XCEditorTools.CopyConfigToSteamAssets)]
        public static void CopyDirToAndroidBuild()
        {
            string tgtDir = XCPathConfig.GetGameConfigDir(true);
            string sourceDir = XCPathConfig.GetGameConfigDir();
            FileTool.CopyDirAll(sourceDir, tgtDir);
        }

        private static void CopyDirToWindowBuild(string buildDir)
        {
            string tgtDir = XCPathConfig.GetWindowBuildResDir();
            string sourceDir = XCPathConfig.GetBuildExtraResDir();
            FileTool.CopyDirAll(sourceDir, tgtDir);
            Debug.Log($"复制完成！{buildDir} -> {tgtDir}");
        }


    }

}
