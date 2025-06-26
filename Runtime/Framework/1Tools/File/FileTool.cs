using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using OdinSerializer;
using SerializationUtility = OdinSerializer.SerializationUtility;
using UnityEngine.XR;
using XiaoCao;
using UnityEngine.Networking;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Collections;




#if UNITY_EDITOR
using UnityEditor;
#endif

///<see cref="PathTool"/>
public static class FileTool
{
#if UNITY_EDITOR
    public static void OpenDir(string path, bool isAssetPath = false)
    {
        EditorUtility.RevealInFinder(PathTool.GetUpperDir(path));
    }
#endif

    public static void WriteToFile(string str, string filePath, bool autoCreatDir = false)
    {
        if (autoCreatDir)
        {
            CheckFilePathDir(filePath);
        }
        using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
        {
            sw.Write(str);
            sw.Close();
        }
    }

    public static void WriteLineToFile(List<string> strList, string filePath)
    {
        string tempfile = Path.GetTempFileName();
        using (var writer = new StreamWriter(tempfile))
        {
            foreach (var str in strList)
            {
                writer.WriteLine(str);
            }
        }
        File.Copy(tempfile, filePath, true);
        //删除临时文件
        if (File.Exists(tempfile))
        {
            Debug.Log("删除临时文件: " + tempfile);
            File.Delete(tempfile);
        }
    }

    public static void WriteToFile(byte[] by, string filePath, bool autoCreatDir = false)
    {
        if (autoCreatDir)
        {
            CheckFilePathDir(filePath);
        }

        File.WriteAllBytes(filePath, by);
        Debug.LogFormat("WriteToFile {0}", filePath);
    }

    public static void WriteAllBytes(string filePath, byte[] by, bool autoCreatDir = false)
    {
        if (autoCreatDir)
        {
            CheckFilePathDir(filePath);
        }
        File.WriteAllBytes(filePath, by);
    }

    public static void CheckFilePathDir(string filePath)
    {
        string dirPath = PathTool.GetUpperDir(filePath);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
    }

    public static void SerializeWrite<T>(string path, T data)
    {
        byte[] bytes = SerializationUtility.SerializeValue<T>(data, XCPathConfig.RawFileFormat);
        FileTool.WriteAllBytes(path, bytes, true);
    }

    public static void SerializeWriteJson<T>(string path, T data)
    {
        byte[] bytes = SerializationUtility.SerializeValue<T>(data, DataFormat.JSON);
        FileTool.WriteAllBytes(path, bytes, true);
    }

    public static T DeserializeRead<T>(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        T data = OdinSerializer.SerializationUtility.DeserializeValue<T>(bytes, XCPathConfig.RawFileFormat);
        return data;
    }

    public static T DeserializeReadJson<T>(string path)
    {
        byte[] bytes = ReadByte(path);
        T data = OdinSerializer.SerializationUtility.DeserializeValue<T>(bytes, DataFormat.JSON);
        return data;
    }

    public static byte[] ReadByte(string filePath)
    {
        return File.ReadAllBytes(filePath);
    }

    public static byte[] WWWReadByteSync(string path)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            //阻塞线程
            while (!operation.isDone)
            {

            }
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 获取文件内容
                return request.downloadHandler.data;
            }
            else
            {
                Debug.LogError($"无法加载文件: {request.error}");
                return null;
            }
        }
    }


    public static string WWWAllTextsSync(string path)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            //阻塞线程
            while (!operation.isDone) { }
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 获取文件内容
                return request.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"无法加载文件: {request.error}");
                return null;
            }
        }
    }

    public static IEnumerator CopyStreamingAssetsFileToPersistentData(string fileName)
    {
        string sourcePath = Path.Combine(Application.streamingAssetsPath, fileName);
        string destPath = Path.Combine(Application.persistentDataPath, fileName);

        // 检查目标路径是否已存在文件（避免重复复制）
        if (File.Exists(destPath))
        {
            Debug.Log($"文件已存在：{fileName}");
            //yield break;
        }

        // Android/WebGL平台使用UnityWebRequest加载
        using (UnityWebRequest request = UnityWebRequest.Get(sourcePath))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllBytes(destPath, request.downloadHandler.data);
                Debug.Log($"复制成功：{fileName}");
            }
            else
            {
                Debug.LogError($"复制失败：{request.error}");
            }
        }
    }

    public static List<string> ReadFileLines(string filePath)
    {
        List<string> strList = new List<string>();
        try
        {
            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    strList.Add(reader.ReadLine());
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return strList;
    }

    public static string ReadFileString(string filePath)
    {
        StreamReader sr = null;
        try
        {
            sr = File.OpenText(filePath);
        }
        catch (Exception)
        {
            Debug.LogError(filePath);
            return "";
        }
        return sr.ReadToEnd();
    }
    //读取Url内容
    public static string ReadFileWebUrl(string url)
    {
        WebClient client = new WebClient();
        byte[] buffer = client.DownloadData(new Uri(url));
        string res = Encoding.UTF8.GetString(buffer);
        return res;
    }
    //下载Url内容
    public static string DownloadUrlText(string url, string localFilePath)
    {
        string str = ReadFileWebUrl(url);
        WriteToFile(str, localFilePath);
        return str;
    }

    public static bool IsFileExist(string path)
    {
        return File.Exists(path);
    }

    // 删除文件夹
    public static void DeleteDirectory(string path)
    {
        DirectoryInfo dir = new DirectoryInfo(path);
        if (dir.Exists)
        {
            dir.Delete(true);
            Debug.Log(" Delete " + path);
        }
    }
    //读取贴图
    public static Texture2D LoadTexture(string path, int w = 180, int h = 180)
    {
        if (!IsFileExist(path))
        {
            Debug.LogFormat(" no path {0}", path);
            return null;
        }
        FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        fs.Seek(0, SeekOrigin.Begin);
        byte[] bytes = new byte[fs.Length];
        fs.Read(bytes, 0, (int)fs.Length);
        fs.Close();
        fs.Dispose();

        Texture2D t = new Texture2D(w, h);
        t.LoadImage(bytes);
        return t;
    }
    
    public static bool CopyFolder(string sourcePath, string destinationPath, bool overwrite = true,
    Action<float, string> progressCallback = null)
    {
        string dirName = new DirectoryInfo(sourcePath).Name;
        destinationPath = Path.Combine(destinationPath, dirName);

        // 验证输入参数
        if (!Directory.Exists(sourcePath))
        {
            Debug.LogError($"源文件夹不存在: {sourcePath}");
            return false;
        }

        try
        {
            // 获取源文件夹中的所有文件
            var filePaths = CollectFiles(sourcePath);
            int totalFiles = filePaths.Count;

            if (totalFiles == 0)
            {
                Debug.LogWarning("源文件夹为空");
                return true; // 空文件夹也算复制成功
            }

            // 创建目标文件夹
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            // 复制每个文件
            for (int i = 0; i < totalFiles; i++)
            {
                string sourceFilePath = filePaths[i];
                string relativePath = sourceFilePath.Substring(sourcePath.Length + 1);
                string destinationFilePath = Path.Combine(destinationPath, relativePath);

                // 更新进度
                progressCallback?.Invoke((float)i / totalFiles, relativePath);

                // 创建目标文件所在的目录
                string destinationFileDir = Path.GetDirectoryName(destinationFilePath);
                if (!Directory.Exists(destinationFileDir))
                {
                    Directory.CreateDirectory(destinationFileDir);
                }

                // 复制文件
                File.Copy(sourceFilePath, destinationFilePath, overwrite);
            }

            // 完成进度
            progressCallback?.Invoke(1f, "复制完成");
            Debug.Log($"复制完成！{sourcePath} -> {destinationPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"复制文件夹失败: {e.Message}");
            return false;
        }
    }

    //将目录的文件复制到新目录, 不包括根文件夹
    public static void CopyFolderNoRoot(string srcDir, string destDir)
    {
        Debug.Log($"--- CopyDirAll {srcDir} {destDir}");
        DirectoryInfo diSource = new DirectoryInfo(srcDir);
        DirectoryInfo diTarget = new DirectoryInfo(destDir);
        CopyFolderNoRoot(diSource, diTarget);
    }
    private static void CopyFolderNoRoot(DirectoryInfo source, DirectoryInfo target)
    {
        // Check if the target directory exists, if not, create it
        if (!Directory.Exists(target.FullName))
        {
            Directory.CreateDirectory(target.FullName);
        }

        // Copy each file into it's new directory
        foreach (FileInfo fi in source.GetFiles())
        {
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyFolderNoRoot(diSourceSubDir, nextTargetSubDir);
        }
    }


    public static List<string> CollectFiles(string directory)
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



    /// <summary>
    /// 查找文件
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="searchString">如"*.json"</param>
    /// <returns></returns>
    public static List<string> FindFiles(string directory, string searchString, bool findSubDirectories = true)
    {
        List<string> result = new List<string>();
        try
        {
            // 搜索当前目录
            string[] jsonFiles = Directory.GetFiles(directory, searchString);
            foreach (string file in jsonFiles)
            {
                result.Add(file);
            }

            // 递归搜索子目录
            if (findSubDirectories)
            {
                string[] subDirectories = Directory.GetDirectories(directory);
                foreach (string subDir in subDirectories)
                {
                    result.AddRange(FindFiles(subDir, searchString));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"搜索时发生错误: {ex.Message}");
        }
        return result;
    }

}
