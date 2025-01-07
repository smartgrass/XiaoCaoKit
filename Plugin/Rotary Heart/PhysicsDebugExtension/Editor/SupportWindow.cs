using UnityEditor;

namespace RotaryHeart.Lib.PhysicsExtension
{
    public class SupportWindow : BaseSupportWindow
    {
        const string SUPPORT_FORUM = "https://forum.unity.com/threads/released-physics-debug-extension.481701/";
        const string STORE_LINK = "https://assetstore.unity.com/packages/tools/physics/physics-debug-extension-88947";
        const string ASSET_NAME = "Physics Debug Extension";
        const string VERSION = "1.7.0";

        protected override string SupportForum
        {
            get { return SUPPORT_FORUM; }
        }
        protected override string StoreLink
        {
            get { return STORE_LINK; }
        }
        protected override string AssetName
        {
            get { return ASSET_NAME; }
        }
        protected override string Version
        {
            get { return VERSION; }
        }

        [MenuItem("Tools/Rotary Heart/Physics Debug Extension/About")]
        public static void ShowWindow()
        {
            ShowWindow<SupportWindow>();
        }
    }
}