using NaughtyAttributes;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
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
            staticSetting = ConfigMgr.StaticSettingSo;
        }

        #region StaticSetting

        [Expandable] public StaticSettingSo staticSetting;


        public const int Line1 = 1;

        #endregion


        [Dropdown(nameof(GetBuildTargetName))] public BuildTarget buildTarget;

        public bool IsBuildYooAseet = true;

        public bool IsBuildPackage = true;


        private List<BuildTarget> GetBuildTargetName()
        {
            return new List<BuildTarget>()
            {
                BuildTarget.StandaloneWindows64,
                BuildTarget.Android
            };
        }


        [Button]
        void StartBuld()
        {
            BuildTool.StartBuld(IsBuildYooAseet, IsBuildPackage, buildTarget);
        }

        [Button("删除缓存", 2)]
        void ClearOldData()
        {
            BuildTool.DeleteStreamingAssetsExtraResZip();
            string yoo = Path.Combine(Application.streamingAssetsPath, "yoo");
            if (Directory.Exists(yoo))
            {
                Directory.Delete(yoo, true);
                Debug.Log($"-- 删除streamingAssetsPath yoo 目录成功");
            }
            AssetDatabase.Refresh();
        }

        [Button("检查状态", 2)]
        void CheckResState()
        {
            //OnPreprocessBuild:YooAseets 和 Urp相关的
            //有时构建完闪退 大概率是YooAsset的问题, 手动点一次就好了
            //还有需要排除SkillEditor场景,防止引用换乱,包体大
            
            //ExtraRes.zip
            bool IsMobileOffice = DebugSetting.IsMobileOffice;
            if (buildTarget == BuildTarget.Android)
            {
                //1.检查ExtraRes是否存在
                string zipPath = Path.Combine(Application.streamingAssetsPath, "ExtraRes.zip");
                if (File.Exists(zipPath))
                {
                    Debug.Log($"-- ExtraRes.zip 存在");
                }
                else
                {
                    Debug.LogError($"-- ExtraRes.zip 不存在");
                }
                
                //1.检查两个yoo包资源
                string yooDir = Path.Combine(Application.streamingAssetsPath, "yoo");
                // 1.检查yoo文件夹下有什么文件夹
                if (Directory.Exists(yooDir))
                {
                    var packageNames = YooAssetBuildHelper.GetYooAssetBuildPackageNames();
                    string[] dirs = Directory.GetDirectories(yooDir);
                    if (dirs.Length < packageNames.Count)
                    {
                        packageNames.LogListstr("yoo package");
                        dirs.LogListstr("当前 package");
                        Debug.LogError($"-- {dirs.Length} < {packageNames.Count}, streamingAssets的yoo包数量不对");
                    }
                    else
                    {
                        packageNames.LogListstr("yoo package");
                    }
                }
            }
        }
    }
}