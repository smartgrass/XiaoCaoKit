using UnityEditor;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Toolbars;
#else
using UnityToolbarExtender;
#endif

namespace XiaoCaoEditor
{
    [InitializeOnLoad]
    public class EditorResourceMode
    {
        private const string ButtonStyleName = "Tab middle";
        private const string EditorPrefsKey = "EditorResourceMode";

        private static readonly string[] _resourceModeNames =
        {
            "EditorMode (编辑器模拟模式)",
            "OfflinePlayMode (单机模式)",
            //"HostPlayMode (联机运行模式)",
            //"WebPlayMode (WebGL运行模式)"
        };

        private static int _resourceModeIndex;

#if UNITY_6000_0_OR_NEWER
        private static MainToolbarElement _toolbarElement;
#else
        static class ToolbarStyles
        {
            public static readonly GUIStyle ToolBarButtonGuiStyle;

            static ToolbarStyles()
            {
                ToolBarButtonGuiStyle = new GUIStyle(ButtonStyleName)
                {
                    padding = new RectOffset(2, 8, 2, 2),
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
            }
        }
#endif

        public static int ResourceModeIndex => _resourceModeIndex;

        static EditorResourceMode()
        {
            _resourceModeIndex = EditorPrefs.GetInt(EditorPrefsKey, 0);
            if (_resourceModeIndex < 0 || _resourceModeIndex >= _resourceModeNames.Length)
            {
                _resourceModeIndex = 0;
                EditorPrefs.SetInt(EditorPrefsKey, _resourceModeIndex);
            }

#if UNITY_6000_0_OR_NEWER
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#else
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
#endif
        }

#if UNITY_6000_0_OR_NEWER
        [MainToolbarElement("XiaoCao/Resource Mode",
            defaultDockPosition = MainToolbarDockPosition.Right,
            defaultDockIndex = 0)]
        private static MainToolbarElement CreateToolbarElement()
        {
            _toolbarElement = new MainToolbarDropdown(CreateToolbarContent(), ShowToolbarMenu);
            RefreshToolbarState();
            return _toolbarElement;
        }

        private static MainToolbarContent CreateToolbarContent()
        {
            return new MainToolbarContent(_resourceModeNames[_resourceModeIndex], "资源加载模式");
        }

        private static void ShowToolbarMenu(Rect rect)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var menu = new GenericMenu();
            for (int i = 0; i < _resourceModeNames.Length; i++)
            {
                int index = i;
                menu.AddItem(new GUIContent(_resourceModeNames[i]), index == _resourceModeIndex,
                    () => SetResourceMode(index));
            }

            menu.DropDown(rect);
        }

        private static void SetResourceMode(int selectedIndex)
        {
            if (selectedIndex == _resourceModeIndex)
            {
                return;
            }

            Debug.Log($"更改编辑器资源运行模式 : {_resourceModeNames[selectedIndex]}");
            _resourceModeIndex = selectedIndex;
            EditorPrefs.SetInt(EditorPrefsKey, selectedIndex);
            RefreshToolbarState();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange _)
        {
            RefreshToolbarState();
        }

        private static void RefreshToolbarState()
        {
            if (_toolbarElement == null)
            {
                return;
            }

            _toolbarElement.content = CreateToolbarContent();
            _toolbarElement.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;
        }
#else
        static void OnToolbarGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                GUILayout.Space(10);
                GUILayout.FlexibleSpace();

                int selectedIndex = EditorGUILayout.Popup(
                    "",
                    _resourceModeIndex,
                    _resourceModeNames,
                    ToolbarStyles.ToolBarButtonGuiStyle);

                if (selectedIndex != _resourceModeIndex)
                {
                    Debug.Log($"更改编辑器资源运行模式 : {_resourceModeNames[selectedIndex]}");
                    _resourceModeIndex = selectedIndex;
                    EditorPrefs.SetInt(EditorPrefsKey, selectedIndex);
                }

                GUILayout.FlexibleSpace();
                GUILayout.Space(400);
            }
            EditorGUI.EndDisabledGroup();
        }
#endif
    }
}
