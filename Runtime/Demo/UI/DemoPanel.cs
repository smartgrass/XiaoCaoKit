#if UNITY_EDITOR
#endif
using cfg;
using TMPro;
using XiaoCao;
using XiaoCao.UI;
using Debug = UnityEngine.Debug;
using Toggle = UnityEngine.UI.Toggle;

//部分调试权限
public class DemoPanel : SubPanel
{
    private TMP_Dropdown skinDrop;
    private Toggle mobileInputToggle;

    public override void Init()
    {
        AddSkinDropDown();
        AddTestEnmeyDropDown();
        if (!DebugSetting.IsMobileOffice)
        {
            AddMobileInput();
        }

        AddDebugBtns();
    }


    void AddMobileInput()
    {
        mobileInputToggle = AddToggle(LocalizeKey.MobileInput, OnAddMobileInput);
#if UNITY_EDITOR
        bool isMobileInput = UnityEditor.EditorPrefs.GetBool(LocalizeKey.MobileInput, false);
        mobileInputToggle.SetIsOnWithoutNotify(isMobileInput);
        OnAddMobileInput(isMobileInput); //由于UI的没有显示,所以需要手动调用
#endif
    }

    ///<see cref="KeyInputHud"/>
    void OnAddMobileInput(bool isOn)
    {
        UserInputType userInput = isOn ? UserInputType.Touch : UserInputType.Mouse;
        GameSetting.UserInputType = userInput;
        UIMgr.Inst.OnChangeInputType(userInput);
#if UNITY_EDITOR
        UnityEditor.EditorPrefs.SetBool(LocalizeKey.MobileInput, isOn);
        Debug.Log($"--- GameSetting.UserInputType {GameSetting.UserInputType}");
#endif
    }

    private void AddSkinDropDown()
    {
        skinDrop = AddDropdown(LocalizeKey.SkinList, OnSkinChange, ConfigMgr.Inst.SkinList);
        int index = (int)ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.SkinList, 0);
        skinDrop.SetValueWithoutNotify((int)index);
    }

    private void AddTestEnmeyDropDown()
    {
        skinDrop = AddDropdown(LocalizeKey.TestEnmeyList, OnTestEnmeyChange, ConfigMgr.Inst.TestEnmeyList);
        //int index = (int)ConfigMgr.Inst.LocalSetting.GetValue(LocalizeKey.TestEnmeyList, 0);
        skinDrop.SetValueWithoutNotify(0);
    }

    /// <summary>
    /// 调试按钮一行: 解锁全部关卡 / 击杀敌人 / 解锁全部技能。
    /// </summary>
    private void AddDebugBtns()
    {
        AddButton(LocalizeKey.UnlockAllLevel, OnUnlockAllLevel,
            LocalizeKey.KillEnemyDebug, OnKillAllEnemy,
            LocalizeKey.UnlockAllSkill, OnUnlockAllSkill);
    }

    private void OnUnlockAllLevel()
    {
        GameDebugTool.UnlockAllLevel(GameAllData.playerSaveData);
        SaveMgr.SaveData(GameAllData.playerSaveData);
    }

    private void OnKillAllEnemy()
    {
        foreach (var role in RoleMgr.Inst.roleDic.Values)
        {
            if (!role.IsPlayer && role.Enable && !role.IsDie && role.IsAiOn)
            {
                role.OnDie(new AtkInfo());
            }
        }
    }

    /// <summary>
    /// 解锁全部技能并写入存档, 再通知技能界面刷新格子状态。
    /// </summary>
    private void OnUnlockAllSkill()
    {
        GameDebugTool.UnlockAllSkill(GameAllData.playerSaveData);
        SaveMgr.SaveData(GameAllData.playerSaveData);
        UICanvasMgr.Inst.EventSystem.SendEvent(UIEventNames.SkillChange);
    }


    public void OnTestEnmeyChange(int index)
    {
        string testChangeToEnmey = ConfigMgr.Inst.GetTestEnmeyName(index);
        if (string.IsNullOrEmpty(testChangeToEnmey))
        {
            return;
        }

        Player0 player0 = GameAllData.CommonData.Player0;
        player0.ChangeToTestEnemy(testChangeToEnmey);
    }

    private void OnSkinChange(int index)
    {
        Debug.Log($"--- SkinChange {index}");

        ConfigMgr.Inst.LocalSetting.SetValue(LocalizeKey.SkinList, index);
        GameDataCommon.LocalPlayer.ChangeBody(ConfigMgr.Inst.GetSkinName(index));
    }
}