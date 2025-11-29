using System;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;

/// <summary>
/// <<see cref="PlayerDataProcedure.SetDefaultPlayerSetting"/>
/// </summary>
public class BasicSettingPanel : SubPanel
{
    Toggle toggle;
    Toggle toggle2;
    Toggle toggle3;


    public override void Init()
    {
        //Language
        AddLanguageDropDown();

        toggle = AddToggle(LocalizeKey.LockCam, OnToggleLockCam);
        toggle2 = AddToggle(LocalizeKey.AutoLockEnemy, OnToggleLockEnemy);

        var slider = AddSlider(LocalizeKey.SwapCameraSpeed, OnSliderCameraSpeed, new Vector2(1f, 5f));
        slider.SetValueWithoutNotify(ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.SwapCameraSpeed, 2.5f));

        var anglePowerSlider = AddSlider(LocalizeKey.AnglePower, OnSliderAnglePower, new Vector2(0, 1f));
        anglePowerSlider.SetValueWithoutNotify(ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.AnglePower, 0.5f));

        //toggle3 = AddToggle(LocalizeKey.MouseView, OnToggleMouseView); 
    }

    private void OnSliderAnglePower(float num)
    {
        ConfigMgr.Inst.LocalSetting.SetValue(LocalizeKey.AnglePower, num);
    }

    private void OnSliderCameraSpeed(float num)
    {
        ConfigMgr.Inst.LocalSetting.SetValue(LocalizeKey.SwapCameraSpeed, num);
    }

    public override void RefreshUI()
    {
        toggle.SetIsOnWithoutNotify(ConfigMgr.Inst.LocalSetting.GetBoolValue(LocalizeKey.LockCam));
        toggle2.SetIsOnWithoutNotify(ConfigMgr.Inst.LocalSetting.GetBoolValue(LocalizeKey.AutoLockEnemy));
        //toggle3.SetIsOnWithoutNotify(ConfigMgr.Inst.LocalSetting.GetBoolValue(LocalizeKey.MouseView));
    }

    private void OnToggleMouseView(bool isOn)
    {
        ConfigMgr.Inst.LocalSetting.SetBoolValue(LocalizeKey.MouseView, isOn);
    }

    private void OnToggleLockCam(bool isOn)
    {
        ConfigMgr.Inst.LocalSetting.SetBoolValue(LocalizeKey.LockCam, isOn);
    }

    private void OnToggleLockEnemy(bool isOn)
    {
        ConfigMgr.Inst.LocalSetting.SetBoolValue(LocalizeKey.AutoLockEnemy, isOn);
    }

    private void AddLanguageDropDown()
    {
        List<string> enumList = new List<string>();
        foreach (ELanguage item in Enum.GetValues(typeof(ELanguage)))
        {
            enumList.Add(LocalizeKey.LanguageShowNames[(int)item]);
        }

        var langDropDown = AddDropdown(LocalizeKey.Language, OnLangChange, enumList);
        langDropDown.SetValueWithoutNotify((int)LocalizeMgr.CurLanguage);
    }

    private void OnLangChange(int index)
    {
        LocalizeMgr.ChangeCurLang((ELanguage)index);
    }
}