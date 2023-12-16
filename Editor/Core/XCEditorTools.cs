using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace XiaoCaoEditor
{
    public static class XCEditorTools
    {


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
                Handles.DrawLine(points[i-1], points[i]);
            }
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
                AssetDatabase.CreateAsset(newObject, path);
                AssetDatabase.Refresh();
                objectUsing = AssetDatabase.LoadAssetAtPath<T>(path);
                Debug.Log($"yns Creat");
            }
            return objectUsing;
        }

        public static Object[] ToObjectArray(this IEnumerable<UnityEngine.Object> list)
        {
            return list.ToArray();
        }

    }
}
