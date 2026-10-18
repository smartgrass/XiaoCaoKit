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

    /// <summary>
    /// 切换到相邻页签(方向键导航用), dir>0 切到下一个, dir<0 切到上一个, 循环切换,
    /// 只在页签按钮处于显示状态的页签间切换
    /// </summary>
    public virtual void SwitchTab(int dir)
    {
        var switchable = GetSwitchablePanels();
        if (switchable.Count == 0)
        {
            return;
        }
        int index = switchable.FindIndex(p => p == curPanel);
        if (index < 0)
        {
            index = 0;
        }
        index = (index + dir) % switchable.Count;
        if (index < 0)
        {
            index += switchable.Count;
        }
        SwitchPanel(switchable[index].subPanelName);
    }

    /// <summary>
    /// 获取页签按钮处于显示状态的页签, 隐藏的页签不参与切换
    /// </summary>
    protected List<SubPanel> GetSwitchablePanels()
    {
        return list.FindAll(p => p.TabBtn != null && p.TabBtn.gameObject.activeInHierarchy);
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
    [Header("iconSprite 可空")]
    public Sprite iconSprite;
}