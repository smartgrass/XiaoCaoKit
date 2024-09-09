using System;
using System.Collections.Generic;
using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using static DebugPanel;
using Debug = UnityEngine.Debug;

public class DebugPanel : ViewBase
{
    public GameObject btnPrefab;

    public GameObject content;

    public Transform tabTransfrom;

    public Transform pageTransfrom;

    public Button openBtn;



    private bool isLoaded;

    private bool isShowPanel;

    private List<ToggleBtn> _toggleBtns = new List<ToggleBtn>();

    private const string DebugGUI_IsShow = "DebugGUI/IsShow";
    private const string DebugGUI_IsOtherShowing = "DebugGUI/IsOtherShowing";

    private void Awake()
    {
        openBtn.onClick.AddListener(OnOpenBtn);
#if UNITY_EDITOR
        EditorApplication.pauseStateChanged += PauseStateChanged;
#endif
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        EditorApplication.pauseStateChanged -= PauseStateChanged;
#endif
    }
#if UNITY_EDITOR
    private void PauseStateChanged(PauseState state)
    {
        if (state == PauseState.Paused)
        {
            Debug.Log("--- Pause all movement");
            foreach (var item in RoleMgr.Inst.roleDic)
            {
                item.Value.roleData.movement.isMovingThisFrame = false;
            } 
        }
    }
#endif

    void OnOpenBtn()
    {
        isShowPanel = !isShowPanel;
        if (isShowPanel)
        {
            Show();
        }
        else
        {
            Hide();
        }
        DebugGUI_IsOtherShowing.SetKeyBool(isShowPanel);
    }


    public void Init()
    {

    }

    public override void Show()
    {
        Load();
        ReadSetting();
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

        AddToggleBtn(DebugGUI_IsShow, "ShowDebug");

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

    void ReadSetting()
    {
        foreach (var item in _toggleBtns)
        {
            item.Read();
        }
    }

    void AddToggleBtn(string key, string textStr, Action action = null, Page pageType = Page.Main)
    {
        var pageTf = GetPage(pageType);
        var newBtnGo = Instantiate(btnPrefab, pageTf);
        newBtnGo.SetActive(true);
        Button newBtn = newBtnGo.GetComponent<Button>();
        Text text = newBtnGo.GetComponentInChildren<Text>();
        text.resizeTextForBestFit = true;
        ToggleBtn toggleBtn = new ToggleBtn(key, textStr, newBtn, text);
        _toggleBtns.Add(toggleBtn);
        newBtn.onClick.AddListener(() =>
        {
            action?.Invoke();
        });
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

    public class ToggleBtn
    {
        public ToggleBtn(string key, string showName, Button btn, Text text)
        {
            this.key = key;
            this.showName = showName;
            this.btn = btn;
            this.text = text;
            btn.onClick.AddListener(() => { Switch(); });
        }

        public string key;
        public string showName;
        public Button btn;
        public Text text;

        public void Read()
        {
            bool isOn = GetBool();
            SetText(isOn);
        }

        public void Switch()
        {
            bool isOn = !GetBool();
            SetBool(isOn);
            SetText(isOn);
        }

        private void SetText(bool isOn)
        {
            string state = isOn ? " √ " : " X ";
            text.text = $"{showName}\n[{state}]";
        }

        bool GetBool()
        {
            //TODO 非持久化设置暂无
            return PlayerPrefsTool.GetKeyBool(key);
        }

        void SetBool(bool isOn)
        {
            PlayerPrefsTool.SetKeyBool(key, isOn);
        }

    }
}
