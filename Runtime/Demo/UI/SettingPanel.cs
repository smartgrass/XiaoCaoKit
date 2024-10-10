
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
        SubPanel subPanel = AddPanel<SoundPanel>("Sound");
        SubPanel debugPanel = AddPanel<DebugPanel>("DebugPanel");

        subPanel.Show();
        gameObject.SetActive(false);
        Prefabs.gameObject.SetActive(false); 
        IsInited = true;
    }

}


