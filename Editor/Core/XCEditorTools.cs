using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using static UnityEditor.Progress;
using static UnityEngine.EventSystems.EventTrigger;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace XiaoCaoEditor
{
    public static class XCEditorTools
    {
        public const string OpenPath_Sava = "XiaoCao/打开路径/存档位置";
        public const string OpenPath_LuabnExcel = "XiaoCao/打开路径/技能配置表格";
        public const string ExampleWindow_1 = "XiaoCao/XiaoCaoWindow示例";
        public const string ObjectsWindow = "XiaoCao/对象收藏夹";
        public const string ObjectViewWindow = "XiaoCao/对象检查器";


        ///<see cref="EditorAssetsExtend"/>
        public const string AssetCheck = "Assets/Check/";

        public const string XiaoCaoFlux = "XiaoCao/Flux/";

        public const string XiaoCaoLuban = "XiaoCao/Luban/";

        public const string XiaoCaoGameObject = "GameObject/XiaoCao/";



    }

    /// <summary>
    /// GUI /Draw
    /// </summary>
    public static class XCDraw
    {
        public static void DrawBezier(Vector3 begin, Vector3 end, Vector3 handle)
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < 20; i++)
            {
                float t = 1f / 20 * (i + 1);
                var point = MathTool.GetBezierPoint2(begin, end, handle, t);
                points.Add(point);
            }
            Handles.DrawLines(points.ToArray());
        }
        /// <summary>
        /// 将点连成线
        /// </summary>
        /// <param name="points"></param>
        public static void DrawLines(List<Vector3> points)
        {
            int len = points.Count;
            if (len < 2)
            {
                return;
            }
            for (int i = 1; i < len; i++)
            {
                Handles.DrawLine(points[i - 1], points[i]);
            }
        }



        public static Texture2D lineTex;

        public static void DrawLine(Vector2 pointA, Vector2 pointB)
        {
            DrawLine(pointA, pointB, GUI.contentColor, 1f);
        }
        public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
        {
            Matrix4x4 matrix = GUI.matrix;
            bool flag = !lineTex;
            if (flag)
            {
                lineTex = new Texture2D(1, 1);
            }
            Color color2 = GUI.color;
            GUI.color = color;
            float num = Vector3.Angle(pointB - pointA, Vector2.right);
            bool flag2 = pointA.y > pointB.y;
            if (flag2)
            {
                num = -num;
            }
            GUIUtility.ScaleAroundPivot(new Vector2((pointB - pointA).magnitude, width), new Vector2(pointA.x, pointA.y + 0.5f));
            GUIUtility.RotateAroundPivot(num, pointA);
            GUI.DrawTexture(new Rect(pointA.x, pointA.y, 1f, 1f), lineTex);
            GUI.matrix = matrix;
            GUI.color = color2;
        }
    }


    /// <summary>
    /// 资源处理相关
    /// </summary>
    public static class XCAseetTool
    {
        /// <summary>
        /// 查找1个
        /// </summary>
        public static Object FindOneAssetByName(string typeName = "Object", string nameStr = "", string dir = "Assets")
        {
            Object obj = null;
            string[] guids = AssetDatabase.FindAssets($"{nameStr} t:{typeName}", new string[] { dir });
            List<string> paths = new List<string>();
            new List<string>(guids).ForEach(m => paths.Add(AssetDatabase.GUIDToAssetPath(m)));
            if (paths.Count > 0)
                obj = AssetDatabase.LoadAssetAtPath(paths[0], typeof(Object));
            return obj;
        }
        /// <summary>
        /// 查找多个
        /// </summary>
        /// <returns></returns>
        public static List<Object> FindAssetsByName(string typeName = "Object", string nameStr = "", string dir = "Assets")
        {
            List<Object> objList = new List<Object>();
            string[] guids = AssetDatabase.FindAssets($"{nameStr} t:{typeName}", new string[] { dir });
            List<string> paths = new List<string>();
            new List<string>(guids).ForEach(m => paths.Add(AssetDatabase.GUIDToAssetPath(m)));
            for (int i = 0; i < paths.Count; i++)
            {
                objList.Add(AssetDatabase.LoadAssetAtPath(paths[i], typeof(Object)));
            }
            return objList;
        }


        public static T GetOrNewSO<T>(string path) where T : ScriptableObject
        {
            T objectUsing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (objectUsing == null)
            {
                var newObject = ScriptableObject.CreateInstance<T>();
                FileTool.CheckFilePathDir(path);
                AssetDatabase.CreateAsset(newObject, path);
                AssetDatabase.Refresh();
                objectUsing = AssetDatabase.LoadAssetAtPath<T>(path);
                Debug.Log($" Creat");
            }
            return objectUsing;
        }

        public static Object[] ToObjectArray(this IEnumerable<UnityEngine.Object> list)
        {
            return list.ToArray();
        }

    }


    public static class XCAnimatorTool
    {
        public static void CheckAnim(RuntimeAnimatorController runtimeAnim, Dictionary<string, AnimationClip> animDic, int skillId)
        {

            AnimatorController ac = runtimeAnim as AnimatorController;
            if (ac.layers.Length < 1)
            {
                ac.layers = new AnimatorControllerLayer[1];
            }

            AnimatorStateMachine sm = ac.layers[0].stateMachine;

            Dictionary<string, AnimatorState> stateDic = new Dictionary<string, AnimatorState>();
            foreach (var item in sm.states)
            {
                stateDic.Add(item.state.name, item.state);
            }

            int posXIndex = skillId % 10;

            bool isChange = false;
            int i = 0;
            foreach (var kv in animDic)
            {
                string key = kv.Key;
                var value = kv.Value;
                if (!stateDic.ContainsKey(key))
                {
                    //添加
                    isChange = true;
                    Vector3 pos = new Vector3(800 + posXIndex * 100, i * 20, 0);
                    AnimatorState state = sm.AddState(key, pos);
                    state.motion = value;
                    state.AddExitTransition(true);
                    Debug.Log($"--- add {value}");
                }
                else
                {
                    if (stateDic[key].motion != value)
                    {
                        Debug.Log($"--- change {stateDic[key].motion} {value}");
                        stateDic[key].motion = value;
                        isChange = true;
                    }
                }
                i++;
            }

            if (isChange)
            {
                Debug.Log($"anim controller Change ");
                //AssetDatabase.ForceReserializeAssets(new[] { path });
                EditorUtility.SetDirty(ac);
            }
        }
    }


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
    }

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
                if (!isFound &&  fieldType.IsSubclassOf(monoType))
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

    public static class CommandHelper
    {
        public static void ExecuteBatCommand(string batPath)
        {
            string batDirectory = System.IO.Path.GetDirectoryName(batPath);

            // 创建ProcessStartInfo
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = "cmd.exe"; // 使用cmd.exe来执行bat文件
            processInfo.Arguments = "/c cd /d " + batDirectory + " && " + batPath;

            // 重定向标准输出和标准错误流
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;

            // 设置字符集为GBK /UTF-8
            processInfo.StandardOutputEncoding = Encoding.GetEncoding("UTF-8");
            processInfo.StandardErrorEncoding = Encoding.GetEncoding("UTF-8");

            // 保证在Unity编辑器中显示cmd输出窗口
            processInfo.UseShellExecute = false;
            processInfo.CreateNoWindow = true;

            // 启动进程
            Process process = new Process();
            process.StartInfo = processInfo;
            process.Start();


            // 读取标准输出和标准错误流
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            // 等待进程执行完毕
            process.WaitForExit();

            // 输出日志到Unity控制台
            UnityEngine.Debug.Log("Bat Command Output:\n" + output);
        }
    }
}
