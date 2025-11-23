using NaughtyAttributes;
using System.Collections.Generic;
using UnityEditor;
using XiaoCao;
using XiaoCaoEditor;

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
            staticSetting = ConfigMgr.Inst.StaticSettingSo;
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