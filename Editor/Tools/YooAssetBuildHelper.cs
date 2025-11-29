using System;
using System.Collections.Generic;
using UnityEditor;
using YooAsset.Editor;
using YooAsset;
using UnityEditor.Build.Reporting;
using BuildResult = YooAsset.Editor.BuildResult;
using UnityEngine;
using OdinSerializer;
using SerializationUtility = OdinSerializer.SerializationUtility;
using XiaoCao;
using UnityEngine.Rendering;

public static class YooAssetBuildHelper
{
    public static List<BuildResult> BuildYooAseets()
    {
        List<BuildResult> list = new List<BuildResult>();
        foreach (var PackageName in GetYooAssetBuildPackageNames())
        {
            EBuildPipeline BuildPipeline = AssetBundleBuilderSetting.GetPackageBuildPipeline(PackageName);

            BuildParameters buildParameters = BuildPipeline == EBuildPipeline.BuiltinBuildPipeline ?
                GetBuildPara(PackageName) : GetBuildParaRaw(PackageName);

            LogParameters(buildParameters);

            IBuildPipeline pipeline = buildParameters.BuildPipeline == EBuildPipeline.BuiltinBuildPipeline.ToString() ? new BuiltinBuildPipeline() : new RawFileBuildPipeline();
            var buildResult = pipeline.Run(buildParameters, true);
            list.Add(buildResult);
        }
        return list;
    }

    private static BuiltinBuildParameters GetBuildPara(string PackageName)
    {
        BuildTarget BuildTarget = EditorUserBuildSettings.activeBuildTarget;
        EBuildPipeline BuildPipeline = AssetBundleBuilderSetting.GetPackageBuildPipeline(PackageName);
        BuiltinBuildParameters para = new BuiltinBuildParameters();

        para.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        para.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
        para.BuildPipeline = BuildPipeline.ToString();
        para.BuildTarget = BuildTarget;
        //AssetBundleBuilderSetting.GetPackageBuildMode(PackageName, BuildPipeline); ;
        para.BuildMode = EBuildMode.ForceRebuild;
        para.PackageName = PackageName;
        para.PackageVersion = GetDefaultPackageVersion();
        para.EnableSharePackRule = true;
        para.VerifyBuildingResult = true;
        para.FileNameStyle = AssetBundleBuilderSetting.GetPackageFileNameStyle(PackageName, BuildPipeline); ;

        //var buildinFileCopyOption = AssetBundleBuilderSetting.GetPackageBuildinFileCopyOption(PackageName, BuildPipeline);
        para.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
        para.BuildinFileCopyParams = AssetBundleBuilderSetting.GetPackageBuildinFileCopyParams(PackageName, BuildPipeline);
        para.EncryptionServices = null;
        para.CompressOption = AssetBundleBuilderSetting.GetPackageCompressOption(PackageName, BuildPipeline);
        return para;
    }

    private static RawFileBuildParameters GetBuildParaRaw(string PackageName)
    {
        BuildTarget BuildTarget = EditorUserBuildSettings.activeBuildTarget;
        EBuildPipeline BuildPipeline = AssetBundleBuilderSetting.GetPackageBuildPipeline(PackageName);
        var buildMode = AssetBundleBuilderSetting.GetPackageBuildMode(PackageName, BuildPipeline);
        Debug.Log($"--- buildMode {buildMode} ");
        var fileNameStyle = AssetBundleBuilderSetting.GetPackageFileNameStyle(PackageName, BuildPipeline);
        var buildinFileCopyOption = AssetBundleBuilderSetting.GetPackageBuildinFileCopyOption(PackageName, BuildPipeline);
        var buildinFileCopyParams = AssetBundleBuilderSetting.GetPackageBuildinFileCopyParams(PackageName, BuildPipeline);

        RawFileBuildParameters para = new RawFileBuildParameters();
        para.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        para.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
        para.BuildPipeline = BuildPipeline.ToString();
        para.BuildTarget = BuildTarget;
        para.BuildMode = EBuildMode.ForceRebuild;
        para.PackageName = PackageName;
        para.PackageVersion = GetDefaultPackageVersion();
        para.VerifyBuildingResult = true;
        para.FileNameStyle = fileNameStyle;
        para.BuildinFileCopyOption = buildinFileCopyOption;
        para.BuildinFileCopyParams = buildinFileCopyParams;
        para.EncryptionServices = null;
        return para;
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

    static void LogParameters(BuildParameters parameters)
    {
        byte[] bytes = SerializationUtility.SerializeValue(parameters, DataFormat.JSON);
        string str = System.Text.Encoding.UTF8.GetString(bytes);
        str.LogStr($"BuiltinBuildParameters {parameters.PackageName}");
    }

    public static List<string> GetYooAssetBuildPackageNames()
    {
        List<string> result = new List<string>();
        foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
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

