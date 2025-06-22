using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XiaoCao;
using XiaoCaoEditor;
using YooAsset.Editor;

namespace AssetEditor.Editor
{
    public class BuildWindow : XiaoCaoWindow
    {

        [MenuItem(XCEditorTools.XCBuildWindow)]
        public static BuildWindow Open()
        {
            return OpenWindow<BuildWindow>("构建面板");
        }

        public override void OnEnable()
        {
            base.OnEnable();
            buildTarget = EditorUserBuildSettings.activeBuildTarget;
        }

        #region StaticSetting
        [MiniBtn(nameof(GenStaticSetting), "生成", 100)]
        [MiniBtn(nameof(ReadStaticSetting), "读取", 100)]
        public StaticSetting staticSetting;

        public void GenStaticSetting()
        {
            //BuildTool
            string filePath = XCPathConfig.GetGameConfigFile("static.info");
            FileTool.SerializeWrite<StaticSetting>(filePath, staticSetting);
            string debugPath = $"Assets/Ignore/static.json";
            FileTool.SerializeWriteJson<StaticSetting>(debugPath, staticSetting);
            Debug.Log($"--- sava {filePath} {debugPath}");
        }
        public void ReadStaticSetting()
        {
            staticSetting = ConfigMgr.LoadStaticSetting();
        }


        public const int Line1 = 1;

        #endregion


        [Dropdown(nameof(GetBuildTargetName))]
        public BuildTarget buildTarget;

        public bool IsBuildYooAseet = true;

        public bool IsBuildPackage = true;


        private List<BuildTarget> GetBuildTargetName()
        {
            return new List<BuildTarget>() {
                BuildTarget.StandaloneWindows64,
                BuildTarget.Android
            };
        }


        [Button]
        void StartBuld()
        {
            BuildTool.CheckSaveScene();

            bool isAndriod = buildTarget == BuildTarget.Android;

            CIBuildHelper.SwitchPlatform(buildTarget);

            if (!isAndriod)
            {
                BuildTool.ClearStreamingAssets();
            }

            if (IsBuildYooAseet)
            {
                BuildResult result = YooAssetBuildHelper.BuildYooAseets();
                if (!result.Success)
                {
                    Debug.LogError($"--- BuildYooAseet fail");
                    return;
                }
            }

            if (isAndriod)
            {
                BuildTool.CopyDirToAndroidBuild();
            }


            if (IsBuildPackage)
            {
                BuildTool.ProjectBuild();
            }
        }
    }
}
