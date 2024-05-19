#if UNITY_EDITOR
//using AssetEditor.Editor.Window;

// ReSharper disable once CheckNamespace
using NaughtyAttributes;
using UnityEditor;
using XiaoCao;
using XiaoCaoEditor;

namespace AssetEditor.Editor
{
    public class XCToolWindow : XiaoCaoWindow
    {
        [MenuItem(XCEditorTools.XCToolWindow)]
        public static XCToolWindow Open()
        {
            return OpenWindow<XCToolWindow>("XCToolWindow");
        }

        public const int Line1 = 1;


        [Button("选中角色",Line1)]
        void SelectPlayer()
        {
            if (GameDataCommon.Current.gameState ==  GameState.Running )
            {
                Selection.activeGameObject = GameDataCommon.Current.player0.gameObject;
            }
        }
    }
}
#endif