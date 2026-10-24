using System;
using System.Collections.Generic;
using UnityEditor;
using YooAsset.Editor;
using YooAsset;
using BuildResult = YooAsset.Editor.BuildResult;
using UnityEngine;
using XiaoCao;

/// <summary>
/// YooAsset 资源包构建封装（v3 接口）
/// 按构建管线类型组装参数并执行，供 BuildTool / BuildWindow 调用
/// </summary>
public static class YooAssetBuildHelper
{
    /// <summary>
    /// 普通资源包使用的构建管线。v3 的 LegacyBuildPipeline 对应旧版 BuiltinBuildPipeline，
    /// 想换可编程管线改成 EBuildPipeline.ScriptableBuildPipeline 即可。
    /// </summary>
    private const EBuildPipeline DefaultBuildPipeline = EBuildPipeline.LegacyBuildPipeline;

    /// <summary>
    /// 构建所有收集器中配置的资源包，返回每个包的构建结果
    /// </summary>
    public static List<BuildResult> BuildYooAseets()
    {
        List<BuildResult> list = new List<BuildResult>();
        foreach (var packageName in GetYooAssetBuildPackageNames())
        {
            EBuildPipeline buildPipeline = GetPackageBuildPipeline(packageName);
            BuildParameters buildParameters = buildPipeline == EBuildPipeline.RawFileBuildPipeline
                ? (BuildParameters)GetBuildParaRaw(packageName, buildPipeline)
                : GetBuildPara(packageName, buildPipeline);

            LogParameters(buildParameters);

            IBuildPipeline pipeline = CreatePipeline(buildPipeline);
            var buildResult = pipeline.Run(buildParameters, true);
            list.Add(buildResult);
        }
        return list;
    }

    /// <summary>
    /// RawPackage 固定走原生文件构建管线，其余包走默认管线
    /// </summary>
    private static EBuildPipeline GetPackageBuildPipeline(string packageName)
    {
        if (packageName == ResMgr.RawPackage)
        {
            return EBuildPipeline.RawFileBuildPipeline;
        }

        return DefaultBuildPipeline;
    }

    /// <summary>
    /// 按管线类型创建构建管线实例
    /// </summary>
    private static IBuildPipeline CreatePipeline(EBuildPipeline buildPipeline)
    {
        switch (buildPipeline)
        {
            case EBuildPipeline.RawFileBuildPipeline:
                return new RawFileBuildPipeline();
            case EBuildPipeline.ScriptableBuildPipeline:
                return new ScriptableBuildPipeline();
            default:
                return new LegacyBuildPipeline();
        }
    }

    private static LegacyBuildParameters GetBuildPara(string packageName, EBuildPipeline buildPipeline)
    {
        LegacyBuildParameters para = new LegacyBuildParameters();
        FillCommonParameters(para, packageName, buildPipeline);

        para.BuildBundleType = (int)EBundleType.AssetBundle;
        para.EnableSharePackRule = true;
        para.CompressOption = BundleBuilderSetting.GetPackageCompressOption(packageName, buildPipeline.ToString());
        return para;
    }

    private static RawFileBuildParameters GetBuildParaRaw(string packageName, EBuildPipeline buildPipeline)
    {
        RawFileBuildParameters para = new RawFileBuildParameters();
        FillCommonParameters(para, packageName, buildPipeline);

        para.BuildBundleType = (int)EBundleType.RawBundle;
        return para;
    }

    /// <summary>
    /// 填充两种管线共用的构建参数
    /// </summary>
    private static void FillCommonParameters(BuildParameters para, string packageName, EBuildPipeline buildPipeline)
    {
        string pipelineName = buildPipeline.ToString();

        para.BuildOutputRoot = BundleBuilderHelper.GetDefaultBuildOutputRoot();
        para.BundledFileRoot = BundleBuilderHelper.GetStreamingAssetsRoot();
        para.BuildPipeline = pipelineName;
        para.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
        para.PackageName = packageName;
        para.PackageVersion = GetDefaultPackageVersion();
        para.VerifyBuildingResult = true;
        para.FileNameStyle = BundleBuilderSetting.GetPackageFileNameStyle(packageName, pipelineName);
        para.BundledCopyOption = EBundledCopyOption.ClearAndCopyAll;
        para.BundledCopyParams = BundleBuilderSetting.GetPackageBundledCopyParams(packageName, pipelineName);
        // v3 用 ClearBuildCacheFiles 替代旧的 EBuildMode.ForceRebuild
        para.ClearBuildCacheFiles = true;
        para.UseAssetDependencyDB = true;
    }

    public static bool IsAllSucced(List<BuildResult> list)
    {
        if (list.Count == 0)
        {
            return false;
        }
        foreach (var item in list)
        {
            if (item.Success == false)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 打印构建参数，便于排查构建配置问题
    /// </summary>
    static void LogParameters(BuildParameters parameters)
    {
        string str = $"--- Build {parameters.PackageName} version:{parameters.PackageVersion} " +
                     $"pipeline:{parameters.BuildPipeline} bundleType:{parameters.BuildBundleType} " +
                     $"target:{parameters.BuildTarget} output:{parameters.BuildOutputRoot} " +
                     $"bundledRoot:{parameters.BundledFileRoot} fileNameStyle:{parameters.FileNameStyle} " +
                     $"copyOption:{parameters.BundledCopyOption} clearCache:{parameters.ClearBuildCacheFiles}";
        str.LogStr($"BuildParameters {parameters.PackageName}");
    }

    public static List<string> GetYooAssetBuildPackageNames()
    {
        List<string> result = new List<string>();
        foreach (var package in BundleCollectorSettingData.Setting.Packages)
        {
            result.Add(package.PackageName);
        }
        return result;
    }

    private static string GetDefaultPackageVersion()
    {
        int totalMinutes = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
        return DateTime.Now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
    }
}
