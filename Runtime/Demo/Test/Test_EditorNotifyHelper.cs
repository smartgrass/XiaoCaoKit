using System;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VersionControl;
#endif

public static class Test_EditorNotifyHelper
{
    public static void ShowNotification(string msg)
    {
#if UNITY_EDITOR
        var editorAssembly = typeof(EditorGUIUtility).Assembly;
        Type s_SceneHieararchyWindowType = editorAssembly.GetType(EditorWindowTypeName.GameView);
        EditorWindow win =EditorWindow.GetWindow(s_SceneHieararchyWindowType);
        win.ShowNotification(new GUIContent(msg),1.2f);
#endif
    }

#if UNITY_EDITOR
    class EditorWindowTypeName
    {
        public static string InspectorWindow = "UnityEditor.InspectorWindow";
        public static string ProjectBrowser = "UnityEditor.ProjectBrowser";
        public static string ConsoleWindow = "UnityEditor.ConsoleWindow";
        public static string SceneHierarchyWindow = "UnityEditor.SceneHierarchyWindow";
        public static string GameView = "UnityEditor.GameView";
        public static string SceneView = "UnityEditor.SceneView";
    }

#endif
}
