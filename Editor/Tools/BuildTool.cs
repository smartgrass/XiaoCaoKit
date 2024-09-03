using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace XiaoCaoEditor
{
    public  static class BuildTool
    {
        public static string buildPath = null;

        [PostProcessBuild(1)]
        public static void AfterBuild(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Build Success  输出平台: " + target + "  输出路径: " + pathToBuiltProject);
            buildPath = pathToBuiltProject.Substring(0, pathToBuiltProject.LastIndexOf("/"));
            Debug.Log(buildPath);

            CopyDir("PackageConfig");
            Debug.Log("复制完成！");
        }


        static private void CopyDir(string from)
        {
            string copyDir = Path.Combine(Application.dataPath, from);
            string newDirName = Path.GetDirectoryName(copyDir);

            string tgtDir = Path.Combine(buildPath, newDirName);
            FileTool.CopyDirAll(copyDir, tgtDir);
        }

    }

}
