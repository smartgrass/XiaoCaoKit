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
    public class XCToolWindow : XiaoCaoWindow
    {
        public GameObject prefabInstance;
        public GameObject prefab;

        [MenuItem(XCEditorTools.XCToolWindow)]
        static void Open()
        {
            OpenWindow<XCToolWindow>("XCToolWindow");
        }


        [Button]
        public void ReplaceToPrefaB()
        {
            //PrefabUtility.UnpackPrefabInstance(prefabInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            //string newPath = AssetDatabase.GetAssetPath(prefab);



            //PrefabUtility.SaveAsPrefabAssetAndConnect(prefabInstance, newPath, InteractionMode.AutomatedAction);

            Debug.Log($"--- Do");
        }



    }
}
#endif