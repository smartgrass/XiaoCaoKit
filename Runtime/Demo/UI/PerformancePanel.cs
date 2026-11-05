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
        int quality = (int)ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.RenderQuality, QualitySettings.GetQualityLevel());
        QualityDropDown.SetValueWithoutNotify(quality);


        frameRateDropDown = AddDropdown(LocalizeKey.FrameRate, OnFrameRateChange, frameRateList, false);
        //下拉显示当前生效帧率; PC 不限帧(-1)时落到 60 这一档
        int index = frameRateList.IndexOf(GameSetting.GetFrameRate().ToString());
        frameRateDropDown.SetValueWithoutNotify(index < 0 ? frameRateList.IndexOf("60") : index);
    }

    private void OnFrameRateChange(int index)
    {
        int frameRate = int.Parse(frameRateList[index]);
        //走 GameSetting 统一入口, 移动端会同时关掉垂直同步
        GameSetting.SetFrameRate(frameRate);
        ConfigMgr.Inst.LocalSetting.SetValue(LocalizeKey.FrameRate, frameRate);
    }

    private void OnQualityChange(int index)
    {
        QualitySettings.SetQualityLevel(index);
        ConfigMgr.Inst.LocalSetting.SetValue(LocalizeKey.RenderQuality, index);
        //切画质会把 vSyncCount 还原成质量等级里的配置, 这里按当前帧率档位重新应用一次
        GameSetting.SetFrameRate(GameSetting.GetFrameRate());
    }
}