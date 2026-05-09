using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Toolbars;
#else
using UnityToolbarExtender;
#endif

namespace TEngine
{
    [InitializeOnLoad]
    public class SceneSwitchLeftButton
    {
        private static readonly string SceneMain = "main";

        static SceneSwitchLeftButton()
        {
#if !UNITY_6000_0_OR_NEWER
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
#endif
        }

#if UNITY_6000_0_OR_NEWER
        [MainToolbarElement("XiaoCao/Launcher",
            defaultDockPosition = MainToolbarDockPosition.Left,
            defaultDockIndex = 0)]
        private static MainToolbarElement CreateToolbarElement()
        {
            return new MainToolbarButton(
                new MainToolbarContent(
                    "Launcher",
                    EditorGUIUtility.FindTexture("PlayButton"),
                    "Start Scene Launcher"),
                () => SceneHelper.StartScene(SceneMain));
        }
#else
        static readonly string ButtonStyleName = "Tab middle";
        static GUIStyle _buttonGuiStyle;

        static void OnToolbarGUI()
        {
            _buttonGuiStyle ??= new GUIStyle(ButtonStyleName)
            {
                padding = new RectOffset(2, 8, 2, 2),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                    new GUIContent("Launcher", EditorGUIUtility.FindTexture("PlayButton"), "Start Scene Launcher"),
                    _buttonGuiStyle))
            {
                SceneHelper.StartScene(SceneMain);
            }
        }
#endif
    }

    static class SceneHelper
    {
        static string _sceneToOpen;

        public static void StartScene(string sceneName)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            _sceneToOpen = sceneName;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (_sceneToOpen == null ||
                EditorApplication.isPlaying || EditorApplication.isPaused ||
                EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= OnUpdate;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                string[] guids = AssetDatabase.FindAssets("t:scene " + _sceneToOpen, null);
                if (guids.Length == 0)
                {
                    Debug.LogWarning("Couldn't find scene file");
                }
                else
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    EditorSceneManager.OpenScene(scenePath);
                    EditorApplication.isPlaying = true;
                }
            }

            _sceneToOpen = null;
        }
    }
}
