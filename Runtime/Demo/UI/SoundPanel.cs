using UnityEngine;
using XiaoCao;

public class SoundPanel : SubPanel
{

    public bool isSave;

    public const string MainVolumeKey = "MasterVolume";
    public const string MusicVolumeKey = "MusicVolume";
    public const string SFXVolumeKey = "SFXVolume";

    public override void Init()
    {
        //数值调整->slider
        AddSlider(MainVolumeKey, OnMainChange, new Vector2(0, 1), MainVolumeGetter());
        AddSlider(MusicVolumeKey, OnMusicChange, new Vector2(0, 1), MusicVolumeGetter());
        AddSlider(SFXVolumeKey, OnSFXChange, new Vector2(0, 1), SFXVolumeGetter());
        //多选->selectBox

    }


    private void OnMainChange(float value)
    {
        ConfigMgr.LocalSetting.SetValue(MainVolumeKey, value);
        SoundMgr.Inst.SetVolume(MusicVolumeKey, value);
    }    
    
    private void OnMusicChange(float value)
    {
        ConfigMgr.LocalSetting.SetValue(MusicVolumeKey, value);
        SoundMgr.Inst.SetVolume(MusicVolumeKey, value);
    }
        
    private void OnSFXChange(float value)
    {
        ConfigMgr.LocalSetting.SetValue(SFXVolumeKey, value);
        SoundMgr.Inst.SetVolume(SFXVolumeKey, value);
    }

    private float MainVolumeGetter()
    {
        return ConfigMgr.LocalSetting.GetValue(MainVolumeKey, 1);
    }    
    private float MusicVolumeGetter()
    {
        return ConfigMgr.LocalSetting.GetValue(MusicVolumeKey, 1);
    }    
    private float SFXVolumeGetter()
    {
        return ConfigMgr.LocalSetting.GetValue(SFXVolumeKey, 1);
    }
}