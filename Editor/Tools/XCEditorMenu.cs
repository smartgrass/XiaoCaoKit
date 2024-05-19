using DG.Tweening.Plugins.Core.PathCore;
using OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace XiaoCaoEditor
{

    public class XCEditorMenu { }

    /// <summary>
    /// 编辑器菜单栏扩展
    /// </summary>
    public static class XCToolBarMenu
    {
        [MenuItem(XCEditorTools.OpenPath_Sava)]
        static void OpenPath_Sava()
        {
            EditorUtility.RevealInFinder($"{Application.persistentDataPath}/");
        }
        [MenuItem(XCEditorTools.OpenPath_LuabnExcel)]
        static void OpenPath_Excel()
        {
            string path = $"{PathTool.GetUpperDir(Application.dataPath)}/Tools/Config/Datas/SkillSetting.xlsx";
            Debug.Log($"--- OpenPath_Excel {path}");
            EditorUtility.RevealInFinder(path);
        }

        //[MenuItem(XCEditorTools.CheckPackage)]

        static void CheckAndInstallPackage()
        {
            string packageName = "com.tuyoogame.yooasset";

            if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageName) != null)
            {
                Debug.LogError("no package: " + packageName);
                InstallPackage(packageName);
            }
            else
            {
                Debug.Log("Package already installed: " + packageName);
            }

            void InstallPackage(string packageName)
            {
                //UnityEditor.PackageManager.Client.Add(packageName); // 开始安装 package
            }
        }
    }
    /// <summary>
    /// Asset资源扩展
    /// </summary>
    public static class XCAssetMenu
    {
        [MenuItem(XCEditorTools.AssetCheck + "输出Type")]
        private static void LogType()
        {
            Selection.activeObject.GetType().Name.LogStr(Selection.activeObject.name);
        }

        [MenuItem("Assets/Open By Default", false, 2)]
        public static void OpenByDefault()
        {
            string path = System.IO.Path.GetFullPath(AssetDatabase.GetAssetPath(Selection.activeObject));
            Debug.Log($" {path}");
            EditorUtility.OpenWithDefaultApp(path);
        }

        [MenuItem("Assets/Create/ReadMe", false, 20)]
        static void CreateMd()
        {
            // 获取选中的文件夹或文件的路径
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            string dirName = PathTool.GetDirName(selectedPath);

            string codeText = "";

            File.WriteAllText(selectedPath + "/ReadMe." + dirName + ".md", codeText, System.Text.Encoding.UTF8);

            // 刷新Unity资源窗口
            AssetDatabase.Refresh();
        }

        //创建UTF-8代码
        [MenuItem("Assets/Create/C# Script UTF-8", false, 20)]
        static void CreateCodeText()
        {
            // 获取选中的文件夹或文件的路径
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            string codeText = "using UnityEngine;\r\n\r\npublic class NewBehaviourScript1 : MonoBehaviour\r\n{\r\n\r\n}\r\n";

            File.WriteAllText(selectedPath + "/NewBehaviourScript1.cs", codeText, System.Text.Encoding.UTF8);

            // 刷新Unity资源窗口
            AssetDatabase.Refresh();
        }

        [MenuItem(XCEditorTools.AssetCheck + "编码Ansi-> UTF-8")]
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




        ///play相关
        [MenuItem(XCEditorTools.AssetCheck + "复制到_Res下")]
        private static void CopyToSkillPrefab()
        {
            UnityEngine.Object selectedObject = Selection.activeObject;

            string oldPath = AssetDatabase.GetAssetPath (selectedObject);

            FileInfo info = new FileInfo(oldPath);

            string newPath = Path.Combine(XCPathConfig.GetSkillPrefabDir(RoleType.Player), info.Name);

            Debug.Log($"FLog {info.Name} CopyTo {newPath}");

            AssetDatabase.CopyAsset(oldPath, newPath);
        }


        [MenuItem(XCEditorTools.AssetCheck + "粒子特效 关闭Loop")]
        private static void CloseLoop()
        {
            UnityEngine.Object selectedObject = Selection.activeObject;

            string path = AssetDatabase.GetAssetPath(selectedObject);

            GameObject root = PrefabUtility.LoadPrefabContents(path);

            root.GetComponentsInChildren<ParticleSystem>().ForEach(p =>
            {
                var main = p.main;
                main.loop = false;
            });

            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success)
            {
                Debug.LogError($"预制体：{path} 保存失败!");
            }
            else
            {
                Debug.Log($"预制体：{path} 保存成功!");
            }
        }
    }

    /// <summary>
    /// 组件右键扩展
    /// </summary>
    public static class XCComponentMenu
    {
        [MenuItem("CONTEXT/Component/AutoBind", priority = 10)]
        private static void AutoBind(MenuCommand menuCommand)
        {
            Component component = menuCommand.context as Component;
            var type = component.GetType();
            //mono类
            var monoType = typeof(MonoBehaviour);
            //ui类
            var uiType = typeof(MaskableGraphic);
            //按钮类/交互类
            var selectType = typeof(Selectable);
            //实体对象类
            var ObjectType = typeof(Object);

            var fields = GetFields(type);


            foreach (var field in fields)
            {
                Type fieldType = field.FieldType;

                bool isFound = false;

                if (fieldType.IsSubclassOf(uiType) || fieldType.IsSubclassOf(selectType) || fieldType.IsSubclassOf(ObjectType))
                {
                    //ui 更具名字查找      
                    Object value = field.GetValue(component) as Object;
                    if (value == null)
                    {
                        Transform tf = component.transform.FindChildEx(field.Name);

                        if (tf)
                        {
                            if (field.FieldType == typeof(GameObject))
                            {
                                field.SetValue(component, tf.gameObject);
                            }
                            else
                            {
                                var findComponent = tf.GetComponentInChildren(field.FieldType, true);
                                field.SetValue(component, findComponent);
                            }
                            isFound = true;
                        }
                    }
                    Debug.Log($"--- {field.FieldType} {field.Name}");
                }

                // 属于 Mono 查找和赋值
                if (!isFound && fieldType.IsSubclassOf(monoType))
                {
                    object value = field.GetValue(component);
                    if (value == null)
                    {
                        var findComponent = component.transform.GetComponentInChildren(field.FieldType, true);

                        field.SetValue(component, findComponent);

                        Debug.Log($"--- {field.Name} {findComponent}");
                    }
                }
            }

            EditorUtility.SetDirty(component);

            static List<FieldInfo> GetFields(Type type)
            {
                List<FieldInfo> fields = new List<FieldInfo>();
                // 如果type继承自 MonoBehaviour,那么递归到此为止
                while (type != null && type != typeof(MonoBehaviour) && type != typeof(object))
                {
                    fields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                    type = type.BaseType;
                }
                return fields;
            }
        }
    }

}
