using System.IO;
using Unity.VisualScripting.IonicZip;
using UnityEditor;
using UnityEngine;

namespace XiaoCaoEditor
{
    public class SaveZipTool
    {
        public static void Create()
        {
            string buildFolderPath = PathTool.GetProjectPath() + "/Build";

            string destinationPath = PathTool.GetProjectPath() + "/Build2";


            string actGamePath = Path.Combine(destinationPath, "ActGame");
            string zipPath = Path.Combine(destinationPath, "ActGame.zip");

            try
            {
                // 1. 复制Build文件夹到新位置并重命名为ActGame
                CopyDirectory(buildFolderPath, actGamePath);
                Debug.Log($"已成功复制并重命名文件夹: {actGamePath}");

                // 2. 复制指定文件到ActGame文件夹
                CopySpecificFiles(actGamePath);
                Debug.Log("已成功复制指定文件");

                // 3. 压缩ActGame文件夹为ZIP
                CompressToZip(actGamePath, zipPath);
                Debug.Log($"已成功压缩文件夹为: {zipPath}");

                EditorUtility.DisplayDialog("操作完成", "Build文件夹已成功处理并压缩为ZIP", "确定");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"处理过程中发生错误: {e.Message}");
                EditorUtility.DisplayDialog("操作失败", $"处理过程中发生错误: {e.Message}", "确定");
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = true)
        {
            // 创建目标目录
            Directory.CreateDirectory(destinationDir);

            // 复制文件
            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string destFilePath = Path.Combine(destinationDir, Path.GetFileName(filePath));
                File.Copy(filePath, destFilePath, true);
            }

            // 递归复制子目录
            if (recursive)
            {
                foreach (string subDir in Directory.GetDirectories(sourceDir))
                {
                    string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                    CopyDirectory(subDir, destSubDir, recursive);
                }
            }
        }

        private static void CopySpecificFiles(string destinationDir)
        {
            // 示例：从项目的Assets/Config目录复制配置文件
            string configSourceDir = Path.Combine(Application.dataPath, "Config");
            string configDestDir = Path.Combine(destinationDir, "Config");

            if (Directory.Exists(configSourceDir))
            {
                CopyDirectory(configSourceDir, configDestDir);
            }
            else
            {
                Debug.LogWarning($"配置文件源目录不存在: {configSourceDir}");
            }

            // 可以添加更多需要复制的文件或目录
            // 例如：复制某个特定文件
            string specificFilePath = Path.Combine(Application.dataPath, "ImportantFile.txt");
            string specificFileDestPath = Path.Combine(destinationDir, "ImportantFile.txt");

            if (File.Exists(specificFilePath))
            {
                File.Copy(specificFilePath, specificFileDestPath, true);
            }
        }

        private static void CompressToZip(string directoryPath, string zipPath)
        {
            // 创建压缩文件 - 使用Ionic.Zip库
            using (ZipFile zip = new ZipFile())
            {
                // 设置压缩级别
                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;

                // 获取目录中的所有文件
                string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

                // 计算基础路径，用于从ZIP条目中移除完整路径
                string basePath = Path.GetDirectoryName(directoryPath) + Path.DirectorySeparatorChar;

                foreach (string file in files)
                {
                    // 创建ZIP条目名称（相对路径）
                    string entryName = file.Replace(basePath, "");

                    // 添加文件到压缩包
                    zip.AddFile(file, "");

                    // 设置文件的最后修改时间
                    FileInfo fileInfo = new FileInfo(file);
                    zip[entryName].LastModified = fileInfo.LastWriteTime;
                }

                // 保存压缩文件
                zip.Save(zipPath);
            }
        }
    }

}
