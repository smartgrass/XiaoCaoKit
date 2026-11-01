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
            var fields = GetBindFields(component.GetType());

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

                Object value = FindBindValue(component.transform, fieldType, field.Name);
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
        }

        /// <summary>
        /// 先完整跑一遍 AutoBind 的查找流程, 把"找不到引用"的字段收集起来;
        /// 再按字段类型在组件所在节点下创建同名子节点、挂上对应组件并回填引用。
        /// 资源类(ScriptableObject/贴图等)和抽象类型无法凭空创建, 只记录日志留给手动处理。
        /// </summary>
        [MenuItem("CONTEXT/Component/AutoCreateBind", priority = 11)]
        private static void AutoCreateBind(MenuCommand menuCommand)
        {
            Component component = menuCommand.context as Component;
            var fields = GetBindFields(component.GetType());
            Transform self = component.transform;

            Undo.RecordObject(component, "AutoCreateBind");

            List<FieldInfo> failedFields = new List<FieldInfo>();

            //1. 原流程: 先按"同名字节点 → 自身节点 → 任意子节点"找, 能找到的照旧绑上
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

                Object value = FindBindValue(self, fieldType, field.Name);
                if (!value)
                {
                    failedFields.Add(field);
                    Debug.Log($"--- AutoCreateBind 查找失败 {fieldType.Name} {field.Name}", component);
                    continue;
                }

                field.SetValue(component, value);
                Debug.Log($"--- AutoCreateBind {field.Name} = {value}", component);
            }

            //2. 补建: 为查找失败的字段创建子节点 + 组件, 再回填引用
            int createCount = 0;
            List<string> manualFields = new List<string>();

            foreach (var field in failedFields)
            {
                Object value = CreateBindTarget(self, field.FieldType, field.Name, manualFields);
                if (!value)
                {
                    continue;
                }

                field.SetValue(component, value);
                createCount++;
                Debug.Log($"--- AutoCreateBind 新建并绑定 {field.FieldType.Name} {field.Name} = {value}", component);
            }

            if (manualFields.Count > 0)
            {
                Debug.LogWarning($"--- AutoCreateBind 需手动处理({manualFields.Count}): {string.Join(" / ", manualFields)}", component);
            }

            Debug.Log($"--- AutoCreateBind 完成: 字段 {fields.Count} 个, 新建并绑定 {createCount} 个, 需手动 {manualFields.Count} 个", component);

            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        /// <summary>
        /// 收集类型自身的字段(沿基类往上, 到 MonoBehaviour 为止)
        /// </summary>
        private static List<FieldInfo> GetBindFields(Type type)
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
        /// 例外: Transform / GameObject 这类节点引用只认名字命中的子节点, 不做类型兜底。
        /// </summary>
        private static Object FindBindValue(Transform self, Type fieldType, string fieldName)
        {
            Transform named = self.FindChildEx(fieldName);

            //节点引用必须名字对得上: 按类型兜底只会绑到自身或任意子节点, 看着绑上了其实是错的
            if (fieldType == typeof(GameObject))
            {
                return named ? named.gameObject : null;
            }

            if (typeof(Transform).IsAssignableFrom(fieldType))
            {
                //Transform 命中节点本身; RectTransform 还要求节点本身就是 RectTransform
                return named ? named.GetComponent(fieldType) : null;
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

        /// <summary>
        /// 给查找失败的字段补一个子节点: 同名字节点存在就复用, 否则新建。
        /// 节点引用(Transform / GameObject)只建节点, 其余类型再挂上字段类型的组件。
        /// 无法创建的类型(资源类 / 抽象类 / 非 UI 下的 RectTransform)会把说明记进 manualFields 并返回 null。
        /// </summary>
        private static Object CreateBindTarget(Transform parent, Type fieldType, string fieldName, List<string> manualFields)
        {
            // Transform / GameObject 是节点引用, 建个同名节点就行
            bool isNodeRef = fieldType == typeof(GameObject) || typeof(Transform).IsAssignableFrom(fieldType);

            // 资源类(ScriptableObject/贴图/材质等)、抽象类型和泛型无法当作组件挂到子节点
            if (!isNodeRef && (!typeof(Component).IsAssignableFrom(fieldType) || fieldType.IsAbstract || fieldType.ContainsGenericParameters))
            {
                manualFields.Add($"{fieldType.Name} {fieldName}");
                return null;
            }

            // RectTransform 只能存在于 UI 层级下
            if (fieldType == typeof(RectTransform) && !(parent is RectTransform))
            {
                manualFields.Add($"RectTransform {fieldName}");
                return null;
            }

            // 同名字节点已存在就复用
            Transform named = parent.FindChildEx(fieldName);
            GameObject go = named ? named.gameObject : CreateChildObject(parent, fieldName);

            if (fieldType == typeof(GameObject))
            {
                return go;
            }

            if (fieldType == typeof(Transform))
            {
                return go.transform;
            }

            // RectTransform 字段要求节点本身就是 UI 节点, 普通 Transform 不转换(转换容易出问题)
            if (fieldType == typeof(RectTransform))
            {
                RectTransform rect = go.GetComponent<RectTransform>();
                if (!rect)
                {
                    manualFields.Add($"RectTransform {fieldName}(同名字节点不是 UI 节点)");
                    return null;
                }
                return rect;
            }

            Component added = go.GetComponent(fieldType);
            if (!added)
            {
                added = Undo.AddComponent(go, fieldType);
            }

            if (!added)
            {
                // 建不出组件就把多余的节点撤掉, 避免留下空物体
                if (!named)
                {
                    Undo.DestroyObjectImmediate(go);
                }
                manualFields.Add($"{fieldType.Name} {fieldName}");
                return null;
            }

            return added;
        }

        /// <summary>
        /// 在父节点下新建一个子节点(UI 层级下用 RectTransform), 并登记撤销
        /// </summary>
        private static GameObject CreateChildObject(Transform parent, string childName)
        {
            GameObject go = parent is RectTransform
                ? new GameObject(childName, typeof(RectTransform))
                : new GameObject(childName);

            Undo.RegisterCreatedObjectUndo(go, "AutoCreateBind");
            go.transform.SetParent(parent, false);
            return go;
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
