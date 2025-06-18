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
    public class BuildWindow : XiaoCaoWindow
    {
        public StaticSetting staticSetting;
        public const int Line1 = 1;


        [MenuItem(XCEditorTools.XCBuildWindow)]
        public static BuildWindow Open()
        {
            return OpenWindow<BuildWindow>("构建面板");
        }


        [Button("写入", Line1)]
        public void GenStaticSetting()
        {
            //BuildTool
            string filePath = XCPathConfig.GetGameConfigFile("static.info");
            FileTool.SerializeWrite<StaticSetting>(filePath, staticSetting);
            string debugPath = $"Assets/Ignore/static.json";
            FileTool.SerializeWriteJson<StaticSetting>(debugPath, staticSetting);
            Debug.Log($"--- sava {filePath} {debugPath}");
        }
        [Button("读取", Line1)]
        public void ReadStaticSetting()
        {
            staticSetting = ConfigMgr.LoadStaticSetting();
        }

    }
}
#endif