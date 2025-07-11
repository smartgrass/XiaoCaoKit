﻿using System.IO;
using XiaoCao;
using System;
using DG.Tweening.Plugins.Core.PathCore;
using UnityEngine;
using Path = System.IO.Path;
using NUnit.Framework;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

public static class PathTool
{
    /// <summary>
    /// 获取文件夹名
    /// </summary>
    /// <param name="selectedPath"></param>
    /// <returns></returns>
    public static string GetDirName(string selectedPath)
    {
        DirectoryInfo info = new DirectoryInfo(selectedPath);
        return info.Name;
    }

    /// <summary>
    /// 获取文件夹路径
    /// </summary>
    public static string GetDirFullPath(string selectedPath)
    {
        return System.IO.Path.GetDirectoryName(selectedPath);
    }

    /// <summary>
    /// 获取上级级目录
    /// </summary>
    public static string GetUpperDir(string path)
    {
        var upperPath = Directory.GetParent(path)?.FullName;
        return upperPath;
    }


    public static List<string> GetDirAllFileName(string dirPath)
    {
        List<string> fileNames = new List<string>();

        // 获取目录下的所有文件信息  
        string[] files = Directory.GetFiles(dirPath);
        foreach (string file in files)
        {
            string extension = Path.GetExtension(file);
            if (extension != ".meta")
            {
                // 获取文件名（不包含路径和后缀）  
                string fileName = Path.GetFileNameWithoutExtension(file);
                fileNames.Add(fileName);
            }
        }
        return fileNames;
    }

    public static string GetRegularPath(string path)
    {
        return path.Replace('\\', '/').Replace("\\", "/"); //替换为Linux路径格式
    }

    public static string FullPathToAssetsPath(string fullPath)
    {
#if  NET_STANDARD_2_1
        return Path.Combine("Assets", Path.GetRelativePath(Application.dataPath, fullPath));
#else
        fullPath = GetRegularPath(fullPath);
        return "Assets" + fullPath.RemoveHead(Application.dataPath);
#endif
    }

    /// <summary>
    /// 转换Unity资源路径为文件的绝对路径
    /// </summary>
    public static string AssetPathToFullPath(string assetPath)
    {
        string projectPath = GetProjectPath();
        return $"{projectPath}/{assetPath}";
    }

    public static string GetProjectPath()
    {
        return Directory.GetParent(Application.dataPath).FullName;
    }

    //第二种写法
    public static string GetProjectPath2()
    {
        string projectPath = Path.GetDirectoryName(Application.dataPath);
        return GetRegularPath(projectPath);
    }

#if UNITY_EDITOR
    //获取Asset的ResourcePath
    public static string GetAssetResourcePath(UnityEngine.Object asset)
    {
        if (asset == null)
            return "";
        string path = AssetDatabase.GetAssetPath(asset);
        string str = path.RemoveHead("Assets/Resources/");
        return str.RemoveEnd(".prefab");
    }
#endif
}
