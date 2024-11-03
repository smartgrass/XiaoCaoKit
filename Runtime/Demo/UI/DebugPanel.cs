#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;
#endif
using UnityEngine;
using XiaoCao;
using Debug = UnityEngine.Debug;
using UnityEngine.UI;
using TMPro;

public class DebugPanel : SubPanel
{
    private TMP_Dropdown skinDrop;


    public override void Init()
    {
        AddSkinDropDown();

    }


    private void AddSkinDropDown()
    {
        skinDrop = AddDropdown(LocalizeKey.SkinList, OnSkinChange, ConfigMgr.SkinList);
        int index = (int)ConfigMgr.LocalSetting.GetValue(LocalizeKey.SkinList, 0);
        skinDrop.SetValueWithoutNotify((int)index);
    }

    private void OnSkinChange(int index)
    {
        Debug.Log($"--- SkinChange {index}");
        ConfigMgr.LocalSetting.SetValue(LocalizeKey.SkinList, index);
        GameDataCommon.Current.player0.ChangeBody(ConfigMgr.GetSkinName(index));

    }
}