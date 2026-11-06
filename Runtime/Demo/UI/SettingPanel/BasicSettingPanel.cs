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
    /// <summary>设置项模板行高: VerticalLayoutGroup 不控制子项高度, 直接用模板自身高度</summary>
    private const float DefaultItemHeight = 120f;

    /// <summary>压缩下限: 开关内部 100x100 的图形刚好放得下</summary>
    private const float MinItemHeight = 100f;

    Toggle toggle;
    Toggle toggle2;
    Toggle toggle3;
    Toggle btnTriggerToggle;


    public override void Init()
    {
        //Language
        AddLanguageDropDown();

        toggle = AddToggle(LocalizeKey.LockCam, OnToggleLockCam);
        toggle2 = AddToggle(LocalizeKey.AutoLockEnemy, OnToggleLockEnemy);
        //按钮按下即触发: 关闭后恢复为抬手触发
        btnTriggerToggle = AddToggle(LocalizeKey.BtnTriggerOnDown, OnToggleBtnTriggerOnDown);

        var slider = AddSlider(LocalizeKey.SwapCameraSpeed, OnSliderCameraSpeed, new Vector2(1f, 5f));
        slider.SetValueWithoutNotify(ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.SwapCameraSpeed, 2.5f));

        var anglePowerSlider = AddSlider(LocalizeKey.AnglePower, OnSliderAnglePower, new Vector2(0, 1f));
        anglePowerSlider.SetValueWithoutNotify(ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.AnglePower, 0.5f));

        //相机距离: -1最近, 0默认, 1.5最远, 由 CameraController.TurnMode 映射到实际距离
        var camDistanceSlider = AddSlider(LocalizeKey.CameraDistance, OnSliderCamDistance, new Vector2(-1f, 1.2f));
        camDistanceSlider.SetValueWithoutNotify(ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.CameraDistance, 0f));

        //toggle3 = AddToggle(LocalizeKey.MouseView, OnToggleMouseView); 
    }

    private void OnSliderCamDistance(float num)
    {
        ConfigMgr.Inst.LocalSetting.SetValue(LocalizeKey.CameraDistance, num);
        //调节过程中同步改相机距离, 场景里没有相机时内部自动跳过
        CameraMgr.ApplySettingCamDistance();
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
        btnTriggerToggle.SetIsOnWithoutNotify(GameSetting.BtnTriggerOnDown);
        //toggle3.SetIsOnWithoutNotify(ConfigMgr.Inst.LocalSetting.GetBoolValue(LocalizeKey.MouseView));
        FitItemHeights();
    }

    /// <summary>
    /// 条目数超过容器能容纳的行数时(如新增的"按钮按下即触发"), 按可用高度把每行压紧,
    /// 否则最后一项会画到面板底部之外。行数够用时恢复默认行高
    /// </summary>
    private void FitItemHeights()
    {
        RectTransform container = (RectTransform)transform;
        VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
        if (layout == null || container.childCount == 0)
        {
            return;
        }

        int count = container.childCount;
        float available = container.rect.height - layout.padding.top - layout.padding.bottom;
        if (available <= 0f)
        {
            //面板尚未完成布局, 等下次 Show 再算
            return;
        }

        float target = (available - layout.spacing * (count - 1)) / count;
        target = Mathf.Clamp(target, MinItemHeight, DefaultItemHeight);
        for (int i = 0; i < count; i++)
        {
            RectTransform item = (RectTransform)container.GetChild(i);
            item.sizeDelta = new Vector2(item.sizeDelta.x, target);
        }
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

    /// <summary>
    /// 切换技能/普攻/翻滚/跳跃按钮的触发时机: 开启为按下即触发, 关闭为抬手触发
    /// </summary>
    private void OnToggleBtnTriggerOnDown(bool isOn)
    {
        GameSetting.SetBtnTriggerOnDown(isOn);
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