using DG.Tweening.Plugins.Core.PathCore;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using Path = System.IO.Path;

namespace XiaoCaoEditor
{
    /// <summary>
    /// MenuItem 扩展菜单
    /// </summary>
    public static class EditorAssetsExtend
    {
        [MenuItem("Assets/Check/输出Type")]
        private static void LogType()
        {
            Selection.activeObject.GetType().Name.LogStr(Selection.activeObject.name);
        }

        [MenuItem("Assets/Open By Default", false, 2)]
        public static void OpenByDefault()
        {
            string path = System.IO.Path.GetFullPath(AssetDatabase.GetAssetPath(Selection.activeObject));
            Debug.Log($"yns {path}");
            EditorUtility.OpenWithDefaultApp(path);
        }

        [MenuItem("Assets/Create/ReadMe", false, 20, secondaryPriority = 1)]
        static void CreateMd()
        {
            // 获取选中的文件夹或文件的路径
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            string dirName = PathTool.GetDirName(selectedPath);

            string codeText = "";

            File.WriteAllText(selectedPath + "/ReadMe."+ dirName+".md", codeText, System.Text.Encoding.UTF8);

            // 刷新Unity资源窗口
            AssetDatabase.Refresh();
        }

        //创建UTF-8代码
        [MenuItem("Assets/Create/C# Script UTF-8",false,20,secondaryPriority =10)]
        static void CreateCodeText()
        {
            // 获取选中的文件夹或文件的路径
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            string codeText = "using UnityEngine;\r\n\r\npublic class NewBehaviourScript1 : MonoBehaviour\r\n{\r\n\r\n}\r\n";

            File.WriteAllText(selectedPath + "/NewBehaviourScript1.cs", codeText, System.Text.Encoding.UTF8);

            // 刷新Unity资源窗口
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Check/编码Ansi-> UTF-8")]
        private static void ReadAnsiText()
        {
            // 获取当前在Unity编辑器中选中的对象
            UnityEngine.Object selectedObject = Selection.activeObject;

            if (selectedObject != null && selectedObject is TextAsset)
            {
                TextAsset textAsset = selectedObject as TextAsset;

                string assetPath = AssetDatabase.GetAssetPath(textAsset);

                Encoding encoding = Encoding.GetEncoding(936);

                string text = File.ReadAllText(assetPath, encoding);

                System.IO.File.WriteAllText(assetPath, text, Encoding.UTF8);

                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("请选择一个TextAsset对象进行读取。");
            }
        }
    }


}
