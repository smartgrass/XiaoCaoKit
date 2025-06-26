using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using XiaoCao;

//性能
public class PerformancePanel : SubPanel
{
    //public 
    public TMP_Dropdown frameRateDropDown;

    public TMP_Dropdown QualityDropDown;

    private List<string> frameRateList = new List<string> { "30", "45", "60", "75", "90" };
    private List<string> qualityList = new List<string> { "0", "1", "2", "3", "4" };
    public override void Init()
    {
        Debug.Log($"--- targetFrameRate: {Application.targetFrameRate} QualityLevel:{QualitySettings.GetQualityLevel()}");

        QualityDropDown = AddDropdown(LocalizeKey.RenderQuality, OnQualityChange, qualityList, false);
        int quality = (int)ConfigMgr.LocalSetting.GetValue(LocalizeKey.RenderQuality, QualitySettings.GetQualityLevel());
        QualityDropDown.SetValueWithoutNotify(quality);


        frameRateDropDown = AddDropdown(LocalizeKey.FrameRate, OnFrameRateChange, frameRateList, false);
        float frameRate = ConfigMgr.LocalSetting.GetValue(LocalizeKey.FrameRate, 60);
        int index = (int)((frameRate - 30) / 15);
        frameRateDropDown.SetValueWithoutNotify(index);
    }

    private void OnFrameRateChange(int index)
    {
        int frameRate = int.Parse(frameRateList[index]);
        Application.targetFrameRate = frameRate;
        ConfigMgr.LocalSetting.SetValue(LocalizeKey.FrameRate, frameRate);
    }

    private void OnQualityChange(int index)
    {
        QualitySettings.SetQualityLevel(index);
        ConfigMgr.LocalSetting.SetValue(LocalizeKey.RenderQuality, index);
    }
}
