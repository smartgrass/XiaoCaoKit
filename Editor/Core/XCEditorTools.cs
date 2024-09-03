using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace XiaoCaoEditor
{

    public static class XCEditorTools
    {
        ///<see cref="XCEditorMenu"/>
        public const string AssetCheck = "Assets/Check/";

        public const string XiaoCaoGameObject = "GameObject/XiaoCao/";

        public const string XiaoCaoFlux = "XiaoCao/Flux/";

        public const string XiaoCaoLuban = "XiaoCao/Luban/";

        public const string XiaoCaoPath = "XiaoCao/打开路径/";

        public const string XiaoCaoGenCode = "XiaoCao/GenCode/";

        //ToolBar
        public const string OpenPath_Sava = XiaoCaoPath + "存档位置";
        public const string OpenPath_LuabnExcel = XiaoCaoPath + "配置表格文件夹";

        public const string ExampleWindow_1 = "XiaoCao/XiaoCaoWindow示例";
        public const string XCToolWindow = "XiaoCao/XCToolWindow";
        public const string ObjectsWindow = "XiaoCao/对象收藏夹";
        public const string CheckPackage = "XiaoCao/检查Package"; //XCToolBarMenu.CheckAndInstallPackage

        //其他
        ///打包相关 <see cref="BuildTool"/>
        ///Scene绘制相关 <see cref="XCDraw"/>
        ///资源处理相关 <see cref="XCAseetTool"/>
        ///AnimatorController连线 <see cref="XCAnimatorTool"/>
        ///批命令 <see cref="CommandHelper"/>
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

        /// <summary>
        /// 查询字符在文本中的行数位置
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public static int GetLineNumber(string filePath, string searchString)
        {
            int lineNumber = 0;
            using (StreamReader sr = new StreamReader(filePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    lineNumber++;
                    if (line.Contains(searchString))
                    {
                        return lineNumber;
                    }
                }
            }
            return 0; // 如果未找到，返回0
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
                    Vector3 pos = GetClipPos(skillId, i);
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

        [MenuItem(XCEditorTools.AssetCheck + "排序clip")]
        private static void SortEditrorAnimatorPos()
        {
            var select = Selection.activeObject;
            AnimatorController ac = select as AnimatorController;
            if (ac == null)
            {
                return;
            }
            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            var newStates = sm.states;
            for (int i = 0; i < newStates.Length; i++)
            {
                string name = newStates[i].state.name;
                int index = 0;
                int subIndex = 0;

                var strArr = name.Split('_');
                if (strArr.Length > 1)
                {
                    if (int.TryParse(strArr[0], out int outIndex))
                    {
                        index = outIndex;
                    }
                    if (int.TryParse(strArr[1], out int outSubIndex))
                    {
                        subIndex = outSubIndex;
                    }
                    newStates[i].position = GetClipPos(index, subIndex);
                }
            }

            sm.states = newStates;

            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();     //保存改动的资源
            AssetDatabase.Refresh();

        }

        private static Vector3 GetClipPos(int index, int subIndex)
        {
            int subPosYIndex = index % 10; //个位数

            index = index % 100;

            int posYIndex = index / 10;//十位数

            posYIndex = posYIndex % 20;


            return new Vector3(600 + subPosYIndex * 220, (posYIndex + subIndex / 5f) * 50, 0);
        }
    }


    public static class CommandHelper
    {
        public static string ExecuteBatCommand(string batPath)
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
            return output;
        }
    }
}
