using OdinSerializer.Utilities;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using XiaoCao;

public class SoundPanel : SubPanel
{

    public bool isSave;

    public const string MainVolumeKey = "MasterVolume";
    public const string MusicVolumeKey = "MusicVolume";
    public const string SFXVolumeKey = "SFXVolume";

    public TMP_Dropdown bgmDropDown;

    public override void Init()
    {
        //数值调整->slider
        AddSlider(MainVolumeKey, OnMainChange, new Vector2(0, 1), MainVolumeGetter());
        AddSlider(MusicVolumeKey, OnMusicChange, new Vector2(0, 1), MusicVolumeGetter());
        AddSlider(SFXVolumeKey, OnSFXChange, new Vector2(0, 1), SFXVolumeGetter());
        //多选->selectBox

        List<string> songList = new List<string>();
        songList.Add("--");
        DirectoryInfo directory = new DirectoryInfo($"{XCPathConfig.GetGameConfigDir()}/Bgm");
        var files = directory.GetFiles("*.mp3", SearchOption.TopDirectoryOnly);
        files.ForEach(f =>
        {
            songList.Add(f.Name);
        });

        bgmDropDown = AddDropdown(LocalizeKey.Bgm, OnBgmChange, songList, false);

        string bgmName = LocalizeKey.Bgm.GetKeyString();
        int index = 0;
        if (string.IsNullOrEmpty(bgmName) && songList.Count > 1)
        {
            index = 1;
        }
        else
        {
            index = songList.IndexOf(bgmName);
            if (index < 0)
            {
                index = 0;
            }
        }
        bgmDropDown.SetValueWithoutNotify(index);
    }

    private void OnBgmChange(int index)
    {
        if (index >= 0)
        {
            string bgmName = bgmDropDown.options[index].text;
            LocalizeKey.Bgm.SetKeyString(bgmName);
            SoundMgr.Inst.PlayBgmByName(bgmName);
        }
    }

    private void OnMainChange(float value)
    {
        ConfigMgr.Inst.LocalSetting.SetValue(MainVolumeKey, value);
        SoundMgr.Inst.SetVolume(MusicVolumeKey, value);
    }

    private void OnMusicChange(float value)
    {
        ConfigMgr.Inst.LocalSetting.SetValue(MusicVolumeKey, value);
        SoundMgr.Inst.SetVolume(MusicVolumeKey, value);
    }

    private void OnSFXChange(float value)
    {
        ConfigMgr.Inst.LocalSetting.SetValue(SFXVolumeKey, value);
        SoundMgr.Inst.SetVolume(SFXVolumeKey, value);
    }

    private float MainVolumeGetter()
    {
        return ConfigMgr.Inst.LocalSetting.GetValue(MainVolumeKey, 1);
    }
    private float MusicVolumeGetter()
    {
        return ConfigMgr.Inst.LocalSetting.GetValue(MusicVolumeKey, 0.5f);
    }
    private float SFXVolumeGetter()
    {
        return ConfigMgr.Inst.LocalSetting.GetValue(SFXVolumeKey, 1);
    }
}