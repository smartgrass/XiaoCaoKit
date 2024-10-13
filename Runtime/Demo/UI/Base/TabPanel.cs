using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCao.UI;

public abstract class TabPanel : PanelBase
{
    [NonSerialized]
    public List<SubPanel> list = new List<SubPanel>();

    public UIPrefabs Prefabs;
    public Transform PANELS;
    public Transform TABS;
    public TextMeshProUGUI Title;



    protected T AddPanel<T>(string panelName, string findPanel = "") where T : SubPanel, new()
    {
        T subPanel = new T();

        //SubTitle 名子
        //创建 Tab & subPanel
        if (string.IsNullOrEmpty(findPanel))
        {
            GetSubPanelGo(subPanel);
        }
        else
        {
            FindSubPanel(subPanel, findPanel);
        }


        subPanel.panel = this;
        subPanel.Prefabs = Prefabs;
        subPanel.subPanelName = panelName;

        GameObject tabBtn = Instantiate(Prefabs.btn, TABS);
        var btn = tabBtn.GetComponentInChildren<Button>();
        btn.onClick.AddListener(() =>
        {
            subPanel.Show();
            btn.Select();
        });
        tabBtn.GetComponentInChildren<Button>().Select();
        tabBtn.GetComponentInChildren<TextMeshProUGUI>().BindLocalizer(panelName);

        subPanel.Init();
        list.Add(subPanel);
        return subPanel;
    }

    public void SwitchPanel(string subPanelName)
    {
        SetSubTitle(subPanelName);
        foreach (var subPanel in list)
        {
            if (subPanel.subPanelName != subPanelName)
            {
                subPanel.Hide();
            }
        }

    }

    public SubPanel GetPanel(string subPanelName)
    {
        return list.Find(p => p.subPanelName == subPanelName);
    }

    private void GetSubPanelGo(SubPanel subPanel)
    {
        GameObject panelGo = Instantiate(Prefabs.subPanel, PANELS);
        subPanel.gameObject = panelGo;
    }

    private void FindSubPanel(SubPanel subPanel, string findPanel)
    {
        GameObject panelGo = PANELS.Find(findPanel).gameObject;
        subPanel.gameObject = panelGo;
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
        IsShowing = false;
        UIMgr.Inst.HideView(panelType);
        LocalSetting.SaveSetting();
    }

    public override void Show()
    {
        if (!IsInited)
        {
            Init();
        }
        IsShowing = true;
        gameObject.SetActive(true);
    }

    #endregion
}


