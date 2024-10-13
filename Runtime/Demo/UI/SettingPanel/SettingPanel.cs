
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;


public class SettingPanel : TabPanel
{
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
        SubPanel debugPanel = AddPanel<DebugPanel>("Debug");
       

        mainPanel.Show();
        gameObject.SetActive(false);
        Prefabs.gameObject.SetActive(false); 
        IsInited = true;
    }

    public override void Hide()
    {
        base.Hide();
        LocalSetting.SaveSetting();
    }

}


