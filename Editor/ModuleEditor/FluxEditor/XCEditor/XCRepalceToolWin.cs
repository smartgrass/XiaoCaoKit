#if UNITY_EDITOR
//using AssetEditor.Editor.Window;

// ReSharper disable once CheckNamespace
using NaughtyAttributes;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;

namespace AssetEditor.Editor
{
    public class XCRepalceToolWin : XiaoCaoWindow
    {
        [Label("被替换实例")]
        public GameObject instance;
        
        public GameObject prefab;

        [Label("替换后保存Seq")]
        public bool isSaveSeq;


        public static XCRepalceToolWin Open()
        {
            return OpenWindow<XCRepalceToolWin>("XCRepalceToolWin");
        }


        [Button("替换")]
        public void ReplaceToPrefaB()
        {
            if (prefab == null) { return; }
            if (instance == null) { return; }
            //PrefabUtility.UnpackPrefabInstance(prefabInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            //string newPath = AssetDatabase.GetAssetPath(prefab);
            //PrefabUtility.SaveAsPrefabAssetAndConnect(prefabInstance, newPath, InteractionMode.AutomatedAction);

            var newInst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            newInst.transform.SetParent(instance.transform.parent);
            newInst.gameObject.name = instance.name;
            newInst.transform.position = instance.transform.position;
            newInst.transform.rotation = instance.transform.rotation;
            newInst.transform.localScale = instance.transform.localScale;


            GameObject.DestroyImmediate(instance);

            instance = newInst;

            if (isSaveSeq)
            {
                SaveXCTask.SavaAll();
            }

            Debug.Log($"--- 替换成功");
        }

    }
}
#endif