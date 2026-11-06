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
    /// <summary>
    /// 设置项模板行高: VerticalLayoutGroup 不控制子项高度, 直接用模板自身高度;
    /// 条目放不下时按可用高度等比压缩, 不再保底(保底会把最后一项挤出面板)
    /// </summary>
    private const float DefaultItemHeight = 120f;

    Toggle toggle;
    Toggle toggle2;
    Toggle toggle3;
    Toggle btnTriggerToggle;
    Toggle autoChaseToggle;


    public override void Init()
    {
        //Language
        AddLanguageDropDown();

        var slider = AddSlider(LocalizeKey.SwapCameraSpeed, OnSliderCameraSpeed, new Vector2(1f, 5f));
        slider.SetValueWithoutNotify(ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.SwapCameraSpeed, 2.5f));

        var anglePowerSlider = AddSlider(LocalizeKey.AnglePower, OnSliderAnglePower, new Vector2(0, 1f));
        anglePowerSlider.SetValueWithoutNotify(ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.AnglePower, 0.5f));

        //相机距离: -1最近, 0默认, 1.5最远, 由 CameraController.TurnMode 映射到实际距离
        var camDistanceSlider = AddSlider(LocalizeKey.CameraDistance, OnSliderCamDistance, new Vector2(-1f, 1.2f));
        camDistanceSlider.SetValueWithoutNotify(ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.CameraDistance, 0f));

        toggle = AddToggle(LocalizeKey.LockCam, OnToggleLockCam);
        toggle2 = AddToggle(LocalizeKey.AutoLockEnemy, OnToggleLockEnemy);
        //按钮按下即触发: 关闭后恢复为抬手触发
        btnTriggerToggle = AddToggle(LocalizeKey.BtnTriggerOnDown, OnToggleBtnTriggerOnDown);
        //普攻追击: 关闭后普攻不再自动追向敌人
        autoChaseToggle = AddToggle(LocalizeKey.AutoChase, OnToggleAutoChase);

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
        //默认值统一在 GameSetting 里: 锁定视角默认开, 自动索敌默认关
        toggle.SetIsOnWithoutNotify(GameSetting.LockCam);
        toggle2.SetIsOnWithoutNotify(GameSetting.AutoLockEnemy);
        btnTriggerToggle.SetIsOnWithoutNotify(GameSetting.BtnTriggerOnDown);
        autoChaseToggle.SetIsOnWithoutNotify(GameSetting.AutoChase);
        //toggle3.SetIsOnWithoutNotify(ConfigMgr.Inst.LocalSetting.GetBoolValue(LocalizeKey.MouseView));
        FitItemHeights();
    }

    /// <summary>
    /// 统一条目行高。套了 ScrollView 时固定用模板行高, 条目超出靠滚动查看;
    /// 没套 ScrollView 时才按可用高度压紧, 避免最后一项画到面板底部之外
    /// </summary>
    private void FitItemHeights()
    {
        int count = content.childCount;
        if (count == 0)
        {
            return;
        }

        float target = DefaultItemHeight;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (scroll == null && layout != null)
        {
            float available = content.rect.height - layout.padding.top - layout.padding.bottom;
            if (available <= 0f)
            {
                //面板尚未完成布局, 等下次 Show 再算
                return;
            }

            target = Mathf.Min((available - layout.spacing * (count - 1)) / count, DefaultItemHeight);
        }

        for (int i = 0; i < count; i++)
        {
            RectTransform item = (RectTransform)content.GetChild(i);
            item.sizeDelta = new Vector2(item.sizeDelta.x, target);
        }
    }

    private void OnToggleMouseView(bool isOn)
    {
        ConfigMgr.Inst.LocalSetting.SetBoolValue(LocalizeKey.MouseView, isOn);
    }

    private void OnToggleLockCam(bool isOn)
    {
        GameSetting.SetLockCam(isOn);
    }

    private void OnToggleLockEnemy(bool isOn)
    {
        GameSetting.SetAutoLockEnemy(isOn);
    }

    /// <summary>
    /// 切换技能/普攻/翻滚/跳跃按钮的触发时机: 开启为按下即触发, 关闭为抬手触发
    /// </summary>
    private void OnToggleBtnTriggerOnDown(bool isOn)
    {
        GameSetting.SetBtnTriggerOnDown(isOn);
    }

    /// <summary>
    /// 切换普攻追击: 关闭后普攻不再自动追向敌人(技能里挂了 disableTag 的指令会跳过)
    /// </summary>
    private void OnToggleAutoChase(bool isOn)
    {
        GameSetting.SetAutoChase(isOn);
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