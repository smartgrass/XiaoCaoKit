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
        [MenuItem(XCEditorTools.XiaoCaoLuban+ "打开Luban文件夹")]
        public static void OpenPath_Excel()
        {
            string path = $"{PathTool.GetUpperDir(Application.dataPath)}/Tools/Datas/SkillSetting.xlsx";
            Debug.Log($"--- OpenPath_Excel {path}");
            EditorUtility.RevealInFinder(path);
        }        
        
        [MenuItem(XCEditorTools.OpenPath_GenConfigPath)]
        [MenuItem(XCEditorTools.XiaoCaoLuban+ "打开生成配置位置")]
        public static void OpenGenConfigPath()
        {
            string path = $"{XCPathConfig.GetGameConfigDir()}/main.ini";
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

            string newPath = Path.Combine(XCPathConfig.GetSkillPrefabDir("1"), info.Name);

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
        [MenuItem("CONTEXT/MonoExecute/Execute", priority = 10)]
        private static void Execute(MenuCommand menuCommand)
        {
            MonoExecute component = menuCommand.context as MonoExecute;
            component.Execute();
        }

        [MenuItem("CONTEXT/Transform/GenBoxCollider", priority = 10)]
        private static void GenBoxCollider(MenuCommand menuCommand)
        {
            Transform tf = menuCommand.context as Transform;
            BuildingTool.AddBoxCollider(tf);
        }
        

        [MenuItem("CONTEXT/Component/AutoBind", priority = 10)]
        private static void AutoBind(MenuCommand menuCommand)
        {
            Component component = menuCommand.context as Component;
            var fields = GetFields(component.GetType());

            Undo.RecordObject(component, "AutoBind");

            foreach (var field in fields)
            {
                Type fieldType = field.FieldType;

                //只处理 UnityEngine.Object 派生类型(GameObject / 组件 / 资源)
                if (!typeof(Object).IsAssignableFrom(fieldType))
                {
                    continue;
                }

                //已经绑过的不覆盖
                if (field.GetValue(component) as Object)
                {
                    continue;
                }

                Object value = FindValue(component.transform, fieldType, field.Name);
                if (!value)
                {
                    //找不到就保持为空, 不再静默写 null(否则会误以为绑上了)
                    Debug.LogWarning($"--- AutoBind 未找到 {fieldType.Name} {field.Name}", component);
                    continue;
                }

                field.SetValue(component, value);
                Debug.Log($"--- AutoBind {field.Name} = {value}", component);
            }

            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);

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

            /// <summary>
            /// 按"同名字节点 → 自身节点 → 任意子节点"的顺序找引用, 找不到返回 null。
            /// 自身节点这一档是必需的: CanvasGroup / Image / TMP 这类引用通常就挂在组件所在节点上。
            /// </summary>
            static Object FindValue(Transform self, Type fieldType, string fieldName)
            {
                Transform named = self.FindChildEx(fieldName);

                if (fieldType == typeof(GameObject))
                {
                    return named ? named.gameObject : self.gameObject;
                }

                //1. 字段名命中的子节点(保留原来的按名字匹配逻辑)
                if (named)
                {
                    Component onNamed = named.GetComponentInChildren(fieldType, true);
                    if (onNamed)
                    {
                        return onNamed;
                    }
                }

                //2. 自身节点
                Component onSelf = self.GetComponent(fieldType);
                if (onSelf)
                {
                    return onSelf;
                }

                //3. 兜底: 任意子节点
                return self.GetComponentInChildren(fieldType, true);
            }
        }


        [MenuItem("CONTEXT/Button/ButtonClick", priority = 10)]
        private static void ButtonClick(MenuCommand menuCommand)
        {
            Button button = menuCommand.context as Button;
            button.onClick?.Invoke();
        }
    }


    //资源预处理
    ///<see cref="SpriteAssetPostprocessor"/>

}
