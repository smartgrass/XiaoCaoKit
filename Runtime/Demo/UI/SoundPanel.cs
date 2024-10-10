using UnityEngine;
using XiaoCao;

public class SoundPanel : SubPanel
{

    public bool isSave;

    public const string MainVolumeKey = "mainVolume";

    public override void Init()
    {
        Hide();

        //数值调整->slider
        AddSlider(MainVolumeKey, MainVolumeGetter, OnMainChange, new Vector2(0, 1));

        //多选->selectBox


    }


    private void OnMainChange(float value)
    {
        Debug.Log($"--- OnMainChange {value}");
        ConfigMgr.LocalSetting.SetValue(MainVolumeKey, value);
    }

    private float MainVolumeGetter()
    {
        return ConfigMgr.LocalSetting.GetValue(MainVolumeKey, 1);
    }

}