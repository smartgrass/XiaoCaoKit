using System;
using System.Collections.Generic;
using UnityEditor;
using YooAsset.Editor;
using YooAsset;

public static class YooAssetBuildHelper
{
    public static BuildResult BuildYooAseets()
    {
        var PackageName = GetYooAssetBuildPackageNames()[0];
        BuildTarget BuildTarget = EditorUserBuildSettings.activeBuildTarget;
        EBuildPipeline BuildPipeline = AssetBundleBuilderSetting.GetPackageBuildPipeline(PackageName);


        BuiltinBuildParameters buildParameters = new BuiltinBuildParameters();
        buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
        buildParameters.BuildPipeline = BuildPipeline.ToString();
        buildParameters.BuildTarget = BuildTarget;
        buildParameters.BuildMode = AssetBundleBuilderSetting.GetPackageBuildMode(PackageName, BuildPipeline); ;
        buildParameters.PackageName = PackageName;
        buildParameters.PackageVersion = GetDefaultPackageVersion();
        buildParameters.EnableSharePackRule = true;
        buildParameters.VerifyBuildingResult = true;
        buildParameters.FileNameStyle = AssetBundleBuilderSetting.GetPackageFileNameStyle(PackageName, BuildPipeline); ;

        //var buildinFileCopyOption = AssetBundleBuilderSetting.GetPackageBuildinFileCopyOption(PackageName, BuildPipeline);
        buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
        buildParameters.BuildinFileCopyParams = AssetBundleBuilderSetting.GetPackageBuildinFileCopyParams(PackageName, BuildPipeline);
        buildParameters.EncryptionServices = null;
        buildParameters.CompressOption = AssetBundleBuilderSetting.GetPackageCompressOption(PackageName, BuildPipeline);

        IBuildPipeline pipeline = BuildPipeline == EBuildPipeline.BuiltinBuildPipeline ? new BuiltinBuildPipeline() : new RawFileBuildPipeline();
        var buildResult = pipeline.Run(buildParameters, true);
        return buildResult;
    }

    private static List<string> GetYooAssetBuildPackageNames()
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

