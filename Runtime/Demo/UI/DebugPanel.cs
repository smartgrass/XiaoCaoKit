#if UNITY_EDITOR
#endif
using TMPro;
using XiaoCao;
using Debug = UnityEngine.Debug;
using Toggle = UnityEngine.UI.Toggle;

public class DebugPanel : SubPanel
{
    private TMP_Dropdown skinDrop;
    private Toggle mobileInputToggle;


    public override void Init()
    {
        AddSkinDropDown();
        AddTestEnmeyDropDown();
        if (!DebugSetting.IsMobilePlatform)
        {
            AddMobileInput();
        }
    }


    void AddMobileInput()
    {
        mobileInputToggle = AddToggle(LocalizeKey.MobileInput, OnAddMobileInput);
    }

    ///<see cref="MobileInputHud"/>
    void OnAddMobileInput(bool isOn)
    {
        UserInputType userInput = isOn ? UserInputType.Touch : UserInputType.Touch;
        GameSetting.UserInputType = userInput;
        UIMgr.Inst.OnChangeInputType(userInput);
    }

    private void AddSkinDropDown()
    {
        skinDrop = AddDropdown(LocalizeKey.SkinList, OnSkinChange, ConfigMgr.SkinList);
        int index = (int)ConfigMgr.LocalSetting.GetValue(LocalizeKey.SkinList, 0);
        skinDrop.SetValueWithoutNotify((int)index);
    }
    private void AddTestEnmeyDropDown()
    {
        skinDrop = AddDropdown(LocalizeKey.TestEnmeyList, OnTestEnmeyChange, ConfigMgr.TestEnmeyList);
        //int index = (int)ConfigMgr.LocalSetting.GetValue(LocalizeKey.TestEnmeyList, 0);
        skinDrop.SetValueWithoutNotify(0);
    }

    public void OnTestEnmeyChange(int index)
    {
        string testChangeToEnmey = ConfigMgr.GetTestEnmeyName(index);
        if (string.IsNullOrEmpty(testChangeToEnmey))
        {
            return;
        }
        Player0 player0 = GameAllData.commonData.player0;
        player0.ChangeToTestEnemy(testChangeToEnmey);
    }

    private void OnSkinChange(int index)
    {
        Debug.Log($"--- SkinChange {index}");

        ConfigMgr.LocalSetting.SetValue(LocalizeKey.SkinList, index);
        GameDataCommon.LocalPlayer.ChangeBody(ConfigMgr.GetSkinName(index));

    }
}