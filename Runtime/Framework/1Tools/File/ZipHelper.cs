using UnityEngine;
using System.IO;
using System.Collections;
using Unity.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using Unity.SharpZipLib.Core;

public class ZipHelper
{
    // 解压ZIP文件的协程实现（带进度报告）
    public static IEnumerator ExtractZip(string zipFilePath, string targetDir)
    {
        // 验证输入参数
        if (!File.Exists(zipFilePath))
        {
            yield break;
        }

        string trueTargetDir = $"{targetDir}/{Path.GetFileNameWithoutExtension(zipFilePath)}";

        if (!Directory.Exists(trueTargetDir))
        {
            Directory.CreateDirectory(trueTargetDir);
        }

        using (FileStream fs = File.OpenRead(zipFilePath))
        using (ZipFile zipFile = new ZipFile(fs))
        {
            int totalEntries = (int)zipFile.Count;
            int processedEntries = 0;

            // 遍历ZIP中的所有条目
            foreach (ZipEntry zipEntry in zipFile)
            {
                if (!zipEntry.IsFile)
                    continue; // 跳过目录

                string entryFileName = zipEntry.Name;
                string entryFilePath = Path.Combine(trueTargetDir, entryFileName);

                // 创建目录结构
                string directoryName = Path.GetDirectoryName(entryFilePath);
                if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                // 解压文件
                byte[] buffer = new byte[4096];
                using (Stream zipStream = zipFile.GetInputStream(zipEntry))
                using (FileStream streamWriter = File.Create(entryFilePath))
                {
                    // 使用协程分块读取，避免阻塞主线程
                    long totalBytes = zipEntry.Size;
                    long bytesWritten = 0;

                    while (true)
                    {
                        int bytesRead = zipStream.Read(buffer, 0, buffer.Length);
                        if (bytesRead <= 0)
                            break;

                        streamWriter.Write(buffer, 0, bytesRead);
                        bytesWritten += bytesRead;

                        // 计算当前文件的解压进度
                        float fileProgress = (float)bytesWritten / totalBytes;
                        // 计算整体进度
                        float overallProgress = (processedEntries + fileProgress) / totalEntries;

                        //progressCallback?.Invoke(overallProgress);

                        // 每写入100KB让出控制权给主线程
                        //if (bytesWritten % 102400 < buffer.Length)
                        //    yield return null;
                    }
                }

                processedEntries++;

                // 每处理一个文件让出控制权
                yield return null;
            }
        }
        //completionCallback?.Invoke(true, "解压完成");
    }

    public static bool CompressFolders(string[] sourceFolderPaths, string targetZipPath,
    int compressionLevel = 6, System.Action<float, string> progressCallback = null)
    {
        if (sourceFolderPaths == null || sourceFolderPaths.Length == 0)
        {
            Debug.LogError("源文件夹路径数组为空");
            return false;
        }

        if (string.IsNullOrEmpty(targetZipPath))
        {
            return false;
        }

        string targetDirectory = Path.GetDirectoryName(targetZipPath);
        // 确保目标目录存在
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        try
        {
            // 收集所有文件
            var filePaths = new List<string>();
            foreach (string sourceFolderPath in sourceFolderPaths)
            {
                if (!Directory.Exists(sourceFolderPath))
                {
                    Debug.LogError($"源文件夹不存在: {sourceFolderPath}");
                    continue;
                }
                filePaths.AddRange(CollectFiles(sourceFolderPath));
            }

            int totalFiles = filePaths.Count;

            if (totalFiles == 0)
            {
                Debug.LogWarning("源文件夹为空");
                return false;
            }


            // 删除已存在的目标文件
            if (File.Exists(targetZipPath))
            {
                File.Delete(targetZipPath);
            }

            // 创建压缩文件
            using (ZipOutputStream zipStream = new ZipOutputStream(File.Create(targetZipPath)))
            {
                zipStream.SetLevel(compressionLevel);

                for (int i = 0; i < totalFiles; i++)
                {
                    string filePath = filePaths[i];
                    string relativePath = GetRelativePath(filePath, sourceFolderPaths);

                    progressCallback?.Invoke((float)i / totalFiles, relativePath);

                    ZipEntry zipEntry = new ZipEntry(relativePath);
                    zipEntry.DateTime = File.GetLastWriteTime(filePath);
                    zipStream.PutNextEntry(zipEntry);

                    byte[] buffer = new byte[4096];
                    using (FileStream fileStream = File.OpenRead(filePath))
                    {
                        StreamUtils.Copy(fileStream, zipStream, buffer);
                    }

                    zipStream.CloseEntry();
                }

                zipStream.IsStreamOwner = true;
                zipStream.Close();
            }

            progressCallback?.Invoke(1f, "压缩完成");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"压缩失败: {e.Message}");
            return false;
        }
    }

    private static string GetRelativePath(string filePath, string[] sourceFolderPaths)
    {
        foreach (string sourceFolderPath in sourceFolderPaths)
        {
            if (filePath.StartsWith(sourceFolderPath))
            {
                string sourceRoot = Path.GetDirectoryName(sourceFolderPath) + Path.DirectorySeparatorChar;
                return filePath.Substring(sourceRoot.Length);
            }
        }
        return filePath;
    }

    private static List<string> CollectFiles(string directory)
    {
        List<string> filePaths = new List<string>();
        CollectFilesRecursive(directory, filePaths);
        return filePaths;
    }

    private static void CollectFilesRecursive(string directory, List<string> filePaths)
    {
        try
        {
            foreach (string filePath in Directory.GetFiles(directory))
            {
                filePaths.Add(filePath);
            }

            foreach (string subDirectory in Directory.GetDirectories(directory))
            {
                CollectFilesRecursive(subDirectory, filePaths);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"无法读取文件夹: {directory}\n{e.Message}");
        }
    }
}
