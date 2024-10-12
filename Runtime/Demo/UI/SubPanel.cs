using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using XiaoCao.UI;
using Object = UnityEngine.Object;
using Slider = UnityEngine.UI.Slider;

public abstract class SubPanel
{
    public string subPanelName;

    public GameObject gameObject;

    public UIPrefabs Prefabs;

    public TabPanel panel;

    public Transform transform => gameObject.transform;

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
    
    
    public Slider AddSlider(string title, UnityAction<float> onValueChange, Vector2 range,float initValue =1)
    {
        GameObject instance = Object.Instantiate(Prefabs.sliderText, gameObject.transform);
        Slider slider = instance.transform.GetChild(1).GetComponent<Slider>();

        slider.maxValue = range.y;
        slider.minValue = range.x;
        slider.SetValueWithoutNotify(initValue);
        slider.onValueChanged.AddListener(onValueChange);
        
        TextMeshProUGUI textMesh = instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        textMesh.gameObject.AddComponent<Localizer>().SetLocalize(title);
        return slider;
    }

    public Dropdown AddDropdown(string title, UnityAction<int> onValueChange, List<string> contents){
        GameObject dropdownPrefab = Prefabs.dropDown;
        GameObject instance = Object.Instantiate(dropdownPrefab, gameObject.transform);
        Dropdown dropdown = instance.GetComponentInChildren<Dropdown>();
        
        TextMeshProUGUI textMesh = instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        textMesh.gameObject.AddComponent<Localizer>().SetLocalize(title);
        
        
        dropdown.ClearOptions();
        dropdown.AddOptions(contents);
        dropdown.onValueChanged.AddListener(onValueChange);
        return dropdown;
    }

    public void AddToggle(string title, UnityAction<bool> onValueChange){
        GameObject togglePrefab = Prefabs.toggle;
        GameObject instance = Object.Instantiate(togglePrefab, gameObject.transform);
        Toggle toggle = instance.GetComponentInChildren<Toggle>();
        
        TextMeshProUGUI textMesh = instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        textMesh.gameObject.AddComponent<Localizer>().SetLocalize(title);

        toggle.onValueChanged.AddListener(onValueChange);
    }
    
    public void AddButton(string title, UnityAction onClick)
    {
        GameObject buttonPrefab = Prefabs.btn;
        GameObject instance = Object.Instantiate(buttonPrefab, gameObject.transform);
        Button button = instance.GetComponent<Button>();

        TextMeshProUGUI textMesh = instance.transform.GetComponentInChildren<TextMeshProUGUI>();
        textMesh.gameObject.AddComponent<Localizer>().SetLocalize(title);
        button.onClick.AddListener(onClick);
    }
    
}
