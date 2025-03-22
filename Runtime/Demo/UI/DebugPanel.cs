#if UNITY_EDITOR
#endif
using TMPro;
using XiaoCao;
using Debug = UnityEngine.Debug;
using Toggle = UnityEngine.UI.Toggle;

public class DebugPanel : SubPanel
{
    private TMP_Dropdown skinDrop;
    private Toggle toggle;

    public override void Init()
    {
        AddSkinDropDown();
        AddMobileInput();
    }


    void AddMobileInput()
    {
        toggle = AddToggle(LocalizeKey.MobileInput, OnAddMobileInput);
    }

    ///<see cref="MobileInputHud"/>
    void OnAddMobileInput(bool isOn)
    {
        var hud = UIMgr.Inst.mobileInputHudTf;
        hud.gameObject.SetActive(isOn);
    }

    private void AddSkinDropDown()
    {
        skinDrop = AddDropdown(LocalizeKey.SkinList, OnSkinChange, ConfigMgr.SkinList);
        int index = (int)ConfigMgr.LocalSetting.GetValue(LocalizeKey.SkinList, 0);
        skinDrop.SetValueWithoutNotify((int)index);
    }

    private void OnSkinChange(int index)
    {
        Debug.Log($"--- SkinChange {index}");
        ConfigMgr.LocalSetting.SetValue(LocalizeKey.SkinList, index);
        GameDataCommon.LocalPlayer.ChangeBody(ConfigMgr.GetSkinName(index));

    }
}