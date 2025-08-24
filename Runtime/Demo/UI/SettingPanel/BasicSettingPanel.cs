﻿using System;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;

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

        var slider = AddSlider(LocalizeKey.SwapCameraSpeed, OnSliderCameraSpeed, new Vector2(0.5f, 3f));
        slider.SetValueWithoutNotify(ConfigMgr.LocalSetting.GetValue(LocalizeKey.SwapCameraSpeed, 1));

        var anglePowerSlider = AddSlider(LocalizeKey.AnglePower, OnSliderAnglePower, new Vector2(0, 1f));
        anglePowerSlider.SetValueWithoutNotify(ConfigMgr.LocalSetting.GetValue(LocalizeKey.AnglePower, 0.5f));

        //toggle3 = AddToggle(LocalizeKey.MouseView, OnToggleMouseView); 
    }

    private void OnSliderAnglePower(float num)
    {
        ConfigMgr.LocalSetting.SetValue(LocalizeKey.AnglePower, num);
    }

    private void OnSliderCameraSpeed(float num)
    {
        ConfigMgr.LocalSetting.SetValue(LocalizeKey.SwapCameraSpeed, num);
    }

    public override void RefreshUI()
    {
        toggle.SetIsOnWithoutNotify(ConfigMgr.LocalSetting.GetBoolValue(LocalizeKey.LockCam));
        toggle2.SetIsOnWithoutNotify(ConfigMgr.LocalSetting.GetBoolValue(LocalizeKey.AutoLockEnemy));
        //toggle3.SetIsOnWithoutNotify(ConfigMgr.LocalSetting.GetBoolValue(LocalizeKey.MouseView));
    }

    private void OnToggleMouseView(bool isOn)
    {
        ConfigMgr.LocalSetting.SetBoolValue(LocalizeKey.MouseView, isOn);
    }

    private void OnToggleLockCam(bool isOn)
    {
        ConfigMgr.LocalSetting.SetBoolValue(LocalizeKey.LockCam, isOn);
    }

    private void OnToggleLockEnemy(bool isOn)
    {
        ConfigMgr.LocalSetting.SetBoolValue(LocalizeKey.AutoLockEnemy, isOn);
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