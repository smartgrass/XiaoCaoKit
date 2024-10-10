#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using XiaoCao;
using Debug = UnityEngine.Debug;

public class DebugPanel : SubPanel
{
    public override void Init()
    {
        Hide();



    }



    void OnOpenBtn()
    {
        // isShowPanel = !isShowPanel;
        // if (isShowPanel)
        // {
        //     Show();
        // }
        // else
        // {
        //     Hide();
        // }
        // DebugGUI_IsOtherShowing.SetKeyBool(isShowPanel);
    }
}