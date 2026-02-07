using GG.Extensions;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using XiaoCao.UI;
using XiaoCaoKit.UI;
using Object = UnityEngine.Object;
using Slider = UnityEngine.UI.Slider;

public abstract class SubPanel
{
    public string subPanelName;

    public GameObject gameObject;

    public UIPrefabs Prefabs;

    public TabStandardPanel standardPanel;

    public Transform transform => gameObject.transform;

    public Button TabBtn { get; set; }

    //public void ShowOrHide()
    //{
    //    if (gameObject.activeSelf)
    //    {
    //        Hide();
    //    }
    //    else
    //    {
    //        Show();
    //    }
    //}

    public void Show()
    {
        gameObject.SetActive(true);
        TabBtn.Select();
        if (TabBtn.transform.parent.TryGetComponent<UIStateChange>(out UIStateChange stateChange))
        {
            Debug.Log($"-- UIStateChange tab");
            stateChange.SetState(1);
        }


        RefreshUI();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (TabBtn.transform.parent.TryGetComponent<UIStateChange>(out UIStateChange stateChange))
        {
            stateChange.SetState(0);
        }
    }

    public abstract void Init();

    public virtual void RefreshUI()
    {
    }

    public TMP_Text AddTextText(string title, string showStr)
    {
        GameObject instance = Object.Instantiate(Prefabs.textText, gameObject.transform);
        TextMeshProUGUI showText = instance.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        showText.text = showStr;

        TextMeshProUGUI textMesh = instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        textMesh.gameObject.AddComponent<Localizer>().SetLocalize(title);
        return showText;
    }


    public Slider AddSlider(string title, UnityAction<float> onValueChange, Vector2 range, float initValue = 1)
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

    public TMP_Dropdown AddDropdown(string title, UnityAction<int> onValueChange, List<string> contents,
        bool needKey = true)
    {
        GameObject dropdownPrefab = Prefabs.dropDown;
        GameObject instance = Object.Instantiate(dropdownPrefab, gameObject.transform);
        TMP_Dropdown dropdown = instance.GetComponentInChildren<TMP_Dropdown>();

        if (needKey)
        {
            TextMeshProUGUI textMesh = instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            textMesh.gameObject.AddComponent<Localizer>().SetLocalize(title);
        }
        else
        {
            TextMeshProUGUI textMesh = instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            textMesh.text = title;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(contents);
        dropdown.onValueChanged.AddListener(onValueChange);
        return dropdown;
    }

    public Toggle AddToggle(string title, UnityAction<bool> onValueChange)
    {
        GameObject togglePrefab = Prefabs.toggle;
        GameObject instance = Object.Instantiate(togglePrefab, gameObject.transform);
        Toggle toggle = instance.GetComponentInChildren<Toggle>();

        TextMeshProUGUI textMesh = instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        textMesh.gameObject.AddComponent<Localizer>().SetLocalize(title);

        toggle.onValueChanged.AddListener(onValueChange);
        return toggle;
    }

    public void AddButton(string title, UnityAction onClick, string tile2 = null, UnityAction onClick2 = null,
        string title3 = null, UnityAction oncClick3 = null)
    {
        GameObject line = Object.Instantiate(Prefabs.btnLine, gameObject.transform);

        for (int i = 0; i < 3; i++)
        {
            var btnGo = line.transform.GetChild(i);
            Button button = btnGo.GetComponentInChildren<Button>();
            TextMeshProUGUI textMesh = btnGo.transform.GetComponentInChildren<TextMeshProUGUI>();

            //根据对应顺序赋值, 如果为null则隐藏btnGo
            string[] titles = { title, tile2, title3 };
            UnityAction[] actions = { onClick, onClick2, oncClick3 };

            if (string.IsNullOrEmpty(titles[i]) || actions[i] == null)
            {
                btnGo.gameObject.SetActive(false);
            }
            else
            {
                textMesh.gameObject.GetOrAddComponent<Localizer>().SetLocalize(titles[i]);
                button.onClick.AddListener(actions[i]);
                btnGo.gameObject.SetActive(true);
            }
        }
    }
}

public class SimpleSubPanel : SubPanel
{
    public override void Init()
    {
    }
}