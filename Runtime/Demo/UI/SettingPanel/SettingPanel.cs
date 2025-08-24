
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;


public class SettingPanel : TabPanel
{
    public Button reloadBtn;
    public override UIPanelType panelType => UIPanelType.SettingPanel;

    public override void Init()
    {
        if (IsInited)
        {
            return;
        }
        base.Init();

        SubPanel mainPanel = AddPanel<BasicSettingPanel>("BasicSetting");
        SubPanel soundPanel = AddPanel<SoundPanel>("Sound");
        SubPanel renderPanel = AddPanel<PerformancePanel>("Performance");
        if (GameSetting.VersionType != GameVersionType.Office)
        {
            SubPanel debugPanel = AddPanel<DemoPanel>("ExtraTest");
        }

        if (GameSetting.VersionType == GameVersionType.Debug)
        {
            SubPanel debugPanel = AddPanel<DebugPanel>("Debug");
        }

        reloadBtn.onClick.AddListener(OnReload);

        mainPanel.Show();
        gameObject.SetActive(false);
        Prefabs.gameObject.SetActive(false);
        IsInited = true;
    }

    private void OnReload()
    {
        GameMgr.Inst.ReloadScene();
    }

    public override void Hide()
    {
        base.Hide();
        LocalSetting.SaveSetting();
    }

    public override void Show()
    {
        base.Show();
        curPanel?.Show();
    }
}


