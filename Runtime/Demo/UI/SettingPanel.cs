
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;

public class SettingPanel : PanelBase
{
    public UIPrefabs Prefabs;
    public Transform PANELS;
    public Transform TABS;
    public TMP_Text subTitle;

    public override UIPanelType panelType => UIPanelType.SettingPanel;

    public override void Init()
    {
        if (IsInited)
        {
            return;
        }
        base.Init();
        SubPanel subPanel = AddPanel<SoundPanel>("Sound");

        subPanel.Show();
        gameObject.SetActive(false);
        Prefabs.gameObject.SetActive(false); 
        IsInited = true;
    }


    protected T AddPanel<T>(string panelName) where T : SubPanel, new()
    {
        T subPanel = new T();

        //SubTitle 名子
        //创建 Tab & subPanel
        GameObject panel = Instantiate(Prefabs.subPanel, PANELS);
        subPanel.gameObject = panel;
        subPanel.panel = this;
        subPanel.Prefabs = Prefabs;
        subPanel.subPanelName = panelName;

        GameObject tabBtn = Instantiate(Prefabs.btn, TABS);
        tabBtn.GetComponent<Button>().onClick.AddListener(subPanel.Show);
        tabBtn.GetComponentInChildren<TextMeshProUGUI>().text = panelName;

        subPanel.Init();
        return subPanel;
    }


    public void SetSubTitle(string str)
    {
        this.subTitle.text = str;  
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


