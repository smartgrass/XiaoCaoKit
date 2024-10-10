using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
    
    
    public void AddSlider(string title, Func<float> volumeGetter, UnityAction<float> onValueChange, Vector2 range)
    {
        GameObject sliderText = GameObject.Instantiate(Prefabs.sliderText, gameObject.transform);
        TextMeshProUGUI textMesh = sliderText.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        textMesh.text = title;
        Slider slider = sliderText.transform.GetChild(1).GetComponent<Slider>();

        slider.maxValue = range.y;
        slider.minValue = range.x;
        slider.value = volumeGetter();

        slider.onValueChanged.AddListener(onValueChange);
    }

    public void AddToggle(string title, UnityAction<bool> onValueChange){
        
    }
    
    public void AddBtn(){
        
    }
    
}
