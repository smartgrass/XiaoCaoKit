#if UNITY_EDITOR
//using AssetEditor.Editor.Window;

// ReSharper disable once CheckNamespace
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;

namespace AssetEditor.Editor
{
    public class FindWindow : XiaoCaoWindow
    {
        [MenuItem(XCEditorTools.XCFindWindow)]
        public static FindWindow Open()
        {
            return OpenWindow<FindWindow>("查询面板");
        }

        public string str;

        public const int Line1 = 1;
        public const int Line2 = 2;

        [Button("文件存在", Line1)]
        public void IsFileExist()
        {
            Debug.Log($"--- IsFileExist {FileTool.IsFileExist(str)} {str}"); ;
        }

        [Button("查询Gui", Line1)]
        void FindGuid()
        {
            var path = AssetDatabase.GUIDToAssetPath(str);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("--- no find");
                return;
            }
            var find = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = find;  
        }

    }
}
#endif