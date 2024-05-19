using System;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;

public class DebugPanel : ViewBase
{
    public GameObject btnPrefab;

    public GameObject content;

    public Transform tabTransfrom;

    public Transform pageTransfrom;

    public Button openBtn;

    private bool isLoaded;

    private bool isShow;

    private void Awake()
    {
        openBtn.onClick.AddListener(OnOpenBtn);
    }

    void OnOpenBtn()
    {
        isShow = !isShow;
        if (isShow)
        {
            Show();          
        }
        else
        {
            Hide();
        }
        DebugGUI.IsShow = !isShow;
    }

    public void Init()
    {

    }

    public override void Show()
    {
        Load();
        content.SetActive(true);
    }


    public override void Hide()
    {
        content.SetActive(false);
    }


    void Load()
    {
        if (isLoaded)
        {
            return;
        }
        isLoaded = true;

        AddPage(Page.Main);

        AddBtn("jumpLevel", OnJumpLevel);
    }

    private void AddPage(Page p)
    {
        var pageTf = GetPage(p);

        var newBtnGo = Instantiate(btnPrefab, tabTransfrom);
        newBtnGo.SetActive(true);
        Button newBtn = newBtnGo.GetComponent<Button>();
        newBtn.onClick.AddListener(() => { SwitchPage(p); });
        Text text = newBtnGo.GetComponentInChildren<Text>();
        text.text = p.ToString();

    }

    private void SwitchPage(Page p)
    {
        var pageTf = GetPage(p);
        int _len = pageTransfrom.transform.childCount;
        for (int i = 0; i < _len; i++)
        {
            var item = pageTransfrom.transform.GetChild(i);
            if (item != pageTf)
            {
                item.gameObject.SetActive(false);
            }
            else
            {
                item.gameObject.SetActive(true);
            }
        }
    }


    private void OnJumpLevel()
    {
        Debug.Log($"--- OnJumpLevel");
    }

    void AddBtn(string textStr, Action action, Page pageType = Page.Main)
    {
        var pageTf = GetPage(pageType);
        var newBtnGo = Instantiate(btnPrefab, pageTf);
        newBtnGo.SetActive(true);
        Button newBtn = newBtnGo.GetComponent<Button>();
        newBtn.onClick.AddListener(() => { action.Invoke(); });
        Text text = newBtnGo.GetComponentInChildren<Text>();
        text.text = textStr;
    }

    private Transform GetPage(Page pageType)
    {
        Transform pageTf = pageTransfrom.transform.Find(pageType.ToString());
        if (pageTf == null)
        {
            pageTf = new GameObject(pageType.ToString()).transform;
            pageTf.SetParent(pageTransfrom.transform);
        }
        return pageTf;
    }

    private enum Page
    {
        Main,
    }
}
