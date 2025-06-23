using NaughtyAttributes;
using System;
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
            staticSetting = ConfigMgr.StaticSettingSo;
        }

        #region StaticSetting
        [Expandable]
        public StaticSettingSo staticSetting;


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
            BuildTool.StartBuld(IsBuildYooAseet, IsBuildPackage, buildTarget);
        }
    }
}
