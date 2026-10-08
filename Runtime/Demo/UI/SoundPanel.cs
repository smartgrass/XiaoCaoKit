using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using XiaoCao;

public class SoundPanel : SubPanel
{

    public bool isSave;

    public const string MainVolumeKey = "Master";
    public const string MusicVolumeKey = "Music";
    public const string SFXVolumeKey = "SFX";

    public TMP_Dropdown bgmDropDown;

    //bgm选项的稳定标识, 与下拉框索引一一对应
    private List<string> bgmValues = new List<string>();

    public override void Init()
    {
        //数值调整->slider
        AddSlider(MainVolumeKey, OnMainChange, new Vector2(0, 1), MainVolumeGetter());
        AddSlider(MusicVolumeKey, OnMusicChange, new Vector2(0, 1), MusicVolumeGetter());
        AddSlider(SFXVolumeKey, OnSFXChange, new Vector2(0, 1), SFXVolumeGetter());
        //多选->selectBox

        bgmDropDown = AddDropdown(LocalizeKey.Bgm, OnBgmChange, BuildBgmShowList(), false);
        SyncBgmDropdown();
    }

    public override void RefreshUI()
    {
        //语言切换后重建选项, 刷新预设项的显示文案
        bgmDropDown.ClearOptions();
        bgmDropDown.AddOptions(BuildBgmShowList());
        SyncBgmDropdown();
    }

    /// <summary>
    /// 生成bgm下拉的显示文本, 同时记录与索引对应的稳定标识
    /// </summary>
    private List<string> BuildBgmShowList()
    {
        //bgm选项: 默认/无 + GameConfig/Bgm子目录下所有bgm, 显示格式: 文件名(子目录名)
        List<string> showList = new List<string>();
        bgmValues.Clear();
        foreach (SoundMgr.BgmOption option in SoundMgr.GetBgmOptions())
        {
            showList.Add(option.Display);
            bgmValues.Add(option.Value);
        }

        return showList;
    }

    /// <summary>
    /// 按当前场景已保存的选择刷新下拉框显示
    /// </summary>
    private void SyncBgmDropdown()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string savedValue = SoundMgr.GetBgmSceneKey(sceneName).GetKeyString();
        int index = bgmValues.IndexOf(savedValue);
        if (index < 0)
        {
            //未保存过时使用"默认"选项
            index = 0;
        }
        bgmDropDown.SetValueWithoutNotify(index);
        bgmDropDown.RefreshShownValue();
    }

    private void OnBgmChange(int index)
    {
        if (index < 0 || index >= bgmValues.Count)
        {
            return;
        }

        //按场景保存选择, 下次进入该场景时读取对应场景的配置
        string sceneName = SceneManager.GetActiveScene().name;
        SoundMgr.GetBgmSceneKey(sceneName).SetKeyString(bgmValues[index]);
        SoundMgr.Inst.PlaySettingBgm();
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
