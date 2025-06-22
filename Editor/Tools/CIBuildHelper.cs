using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CIBuildHelper
{
    public static void ProjectBuildExecute(BuildTarget target)
    {
        //Switch Platform
        SwitchPlatform(target);

        var scenes = GetScenes().ToArray();

        UnityEditor.Build.Reporting.BuildReport report = null;


        BuildOptions buildOptions = EditorUserBuildSettings.development? BuildOptions.Development : BuildOptions.None;

        if (target == BuildTarget.Android)
        {
            report = BuildPipeline.BuildPlayer(scenes, "BuildAndroid/ActGame.apk", BuildTarget.Android, buildOptions);
        }
        else
        {
            report = BuildPipeline.BuildPlayer(scenes, "Build/ActGame.exe", BuildTarget.StandaloneWindows, buildOptions);
        }


        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("打包失败。(" + report.summary.ToString() + ")");
        }

    }

    public static void SwitchPlatform(BuildTarget target)
    {
        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            if (target == BuildTarget.iOS)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            }
            if (target == BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
            else
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows);
            }
        }
    }

    private static List<string> GetScenes()
    {
        List<string> scenes = new List<string>();
        foreach (UnityEditor.EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                if (System.IO.File.Exists(scene.path))
                {
                    Debug.Log("Add Scene (" + scene.path + ")");
                    scenes.Add(scene.path);
                }
            }
        }
        return scenes;
    }

}

