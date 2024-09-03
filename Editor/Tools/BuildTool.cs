using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace XiaoCaoEditor
{
    public  static class BuildTool
    {
        [PostProcessBuild(1)]
        public static void AfterBuild(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Build Success  输出平台: " + target + "  输出路径: " + pathToBuiltProject);
            string buildPath = pathToBuiltProject.Substring(0, pathToBuiltProject.LastIndexOf("/"));
            Debug.Log(buildPath);

            string allConfigDirName = "SceneModeInFo";
            DirectoryInfo appDir = new DirectoryInfo(Application.dataPath);

            string copyDir = Path.Combine(appDir.Parent.FullName, allConfigDirName);
            string tgtDir = Path.Combine(buildPath, allConfigDirName);


            FileTool.CopyDirAll(copyDir, tgtDir);
            Debug.Log("复制完成！");
        }
    }

}
