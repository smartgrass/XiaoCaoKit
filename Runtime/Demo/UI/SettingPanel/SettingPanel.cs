using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCao.UI;


public class SettingPanel : PrefabsTabPanel
{
    public Button reloadBtn;

    public DialogPanel dialogPanel;
    public override UIPanelType PanelType => UIPanelType.SettingPanel;

    //启用方向键导航: 上下键切换主 tab
    public override bool EnableArrowKeyNavigation => true;

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

        //隐藏没有对应子面板的多余 tab 按钮(如 DemoPanel/DebugPanel 未添加时)
        HideUnusedTabBtns();

        //根据 tab 数量自适应按钮高度(3 个 200, 超过 3 个 150), 只改运行时布局
        SettingTabBtnSize.ApplyTo(TABS);

        SwitchPanel("BasicSetting");
        gameObject.SetActive(false);
        prefabs.gameObject.SetActive(false);
        IsInited = true;
    }

    /// <summary>
    /// 隐藏 TABS 下超出已添加子面板数量的 tab 按钮
    /// </summary>
    private void HideUnusedTabBtns()
    {
        int usedCount = list.Count;
        for (int i = 0; i < TABS.childCount; i++)
        {
            TABS.GetChild(i).gameObject.SetActive(i < usedCount);
        }
    }

    private void OnReload()
    {
        dialogPanel.Show("", LocalizeKey.IsExitLevel.ToLocalizeStr(),
            GameMgr.Inst.ReloadScene, GameMgr.Inst.BackHome);
    }

    
    public override void Hide()
    {
        base.Hide();
        LocalSetting.SaveSetting();
    }

    public override void Show(IUIData uiData = null)
    {
        base.Show();
        curPanel?.Show();
    }
}