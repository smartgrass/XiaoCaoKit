using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using XiaoCao;

namespace XiaoCaoEditor
{

    public static class BuildTool
    {
        public static string buildPath = null;

        [PostProcessBuild(1)]
        public static void AfterBuild(BuildTarget target, string pathToBuiltProject)
        {
            Debug.Log("Build Success  输出平台: " + target + "  输出路径: " + pathToBuiltProject);
            buildPath = pathToBuiltProject.Substring(0, pathToBuiltProject.LastIndexOf("/"));
            Debug.Log(buildPath);

            CopyDir();
            Debug.Log("复制完成！");
        }


        static private void CopyDir()
        {
            string copyDir = XCPathConfig.GetGameConfigDir();
            string newDirName = Path.GetDirectoryName(copyDir);

            string tgtDir = Path.Combine(buildPath, newDirName);
            FileTool.CopyDirAll(copyDir, tgtDir);
        }

    }


}
