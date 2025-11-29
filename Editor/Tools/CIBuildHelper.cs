using System;
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


        BuildOptions buildOptions = GetCurrentBuildOptions();

        int index = EditorPrefs.GetInt("BuildIndex", 0);

        EditorPrefs.SetInt("BuildIndex", index + 1);

        if (target == BuildTarget.Android)
        {
            report = BuildPipeline.BuildPlayer(scenes, $"BuildAndroid/ActGame_{index}.apk", BuildTarget.Android,
                buildOptions);
        }
        else
        {
            report = BuildPipeline.BuildPlayer(scenes, "Build/ActGame.exe", BuildTarget.StandaloneWindows,
                buildOptions);
        }


        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("打包失败。(" + report.summary.ToString() + ")");
        }
    }

    /// <summary>
    /// 读取当前Build Settings窗口的配置，转换为BuildOptions枚举
    /// </summary>
    /// <returns>当前配置对应的BuildOptions</returns>
    public static BuildOptions GetCurrentBuildOptions()
    {
        BuildOptions buildOptions = BuildOptions.None;
        buildOptions |= BuildOptions.ShowBuiltPlayer;
        // 1. 基础通用配置（Build Settings窗口的核心勾选项）
        // 开发构建（Development Build）
        if (EditorUserBuildSettings.development)
        {
            buildOptions |= BuildOptions.Development;
        }

        // 自动连接Profiler（Auto Connect Profiler）
        if (EditorUserBuildSettings.connectProfiler)
        {
            buildOptions |= BuildOptions.ConnectWithProfiler;
        }

        // 构建后显示输出文件夹（Show Built Player）
        // if (EditorUserBuildSettings.ShowBuiltPlayer)
        // {
        //     
        // }

        // 2. 可选：Unity 2021+新增的"构建后导出项目"（Export Project）
        // 仅对安卓/iOS等平台生效，对应BuildOptions.AcceptExternalModificationsToPlayer
        if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
        {
            buildOptions |= BuildOptions.AcceptExternalModificationsToPlayer;
        }

        // 3. 其他可选配置（根据需求添加）
        // 严格模式（Strict Mode）：需手动勾选，无原生UI对应，可通过自定义配置控制
        // if (CustomConfig.useStrictMode) buildOptions |= BuildOptions.StrictMode;

        return buildOptions;
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
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,
                    BuildTarget.StandaloneWindows64);
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