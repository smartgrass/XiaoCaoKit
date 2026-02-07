using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCao.UI;

public abstract class TabStandardPanel : StandardPanel
{
    [NonSerialized] public List<SubPanel> list = new List<SubPanel>();
    [NonSerialized] public SubPanel curPanel;
    
    public Transform PANELS;
    public Transform TABS;
    public TextMeshProUGUI Title;


    public override void Init()
    {
        if (IsInited)
        {
            return;
        }

        base.Init();
        IsInited = true;
    }

    public virtual void OnEnable()
    {
        Init();
        if (IsInited)
        {
            curPanel?.RefreshUI();
        }
    }
    

    public virtual void SwitchPanel(string subPanelName)
    {
        SetSubTitle(subPanelName);
        foreach (var subPanel in list)
        {
            if (subPanel.subPanelName != subPanelName)
            {
                subPanel.Hide();
            }
            else
            {
                curPanel = subPanel;
                curPanel.Show();
            }
        }
    }

    public SubPanel GetPanel(string subPanelName)
    {
        return list.Find(p => p.subPanelName == subPanelName);
    }

    public void SetSubTitle(string str)
    {
        this.Title.BindLocalizer(str);
    }

    #region 常规

    public override void OnCloseBtnClick()
    {
        Hide();
    }

    public override void Hide()
    {
        gameObject.SetActive(false);
        UIMgr.Inst.HideView(PanelType);
    }

    public override void Show(IUIData uiData = null)
    {
        if (!IsInited)
        {
            Init();
        }

        gameObject.SetActive(true);
    }

    #endregion
}


[Serializable]
public class TabPanelGroup
{
    public string panelName;
    public GameObject panel;
    public Button tabBtn;
    public Sprite iconSprite;
}