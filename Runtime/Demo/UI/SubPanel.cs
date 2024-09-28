using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using XiaoCao;
using Slider = UnityEngine.UI.Slider;

public abstract class SubPanel
{
    public string subPanelName;

    public GameObject gameObject;

    public UIPrefabs Prefabs;

    public SettingPanel panel;

    public void ShowOrHide()
    {
        if (gameObject.activeSelf)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        panel.SetSubTitle(subPanelName);

    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public abstract void Init();
}

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

    private void AddSlider(string title, Func<float> mainVolumeGetter, UnityAction<float> onMainChange, Vector2 range)
    {
        GameObject sliderText = GameObject.Instantiate(Prefabs.sliderText, gameObject.transform);
        TextMeshProUGUI textMesh = sliderText.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        textMesh.text = title;
        Slider slider = sliderText.transform.GetChild(1).GetComponent<Slider>();

        slider.maxValue = range.y;
        slider.minValue = range.x;
        slider.value = mainVolumeGetter();

        slider.onValueChanged.AddListener(onMainChange);
    }
}