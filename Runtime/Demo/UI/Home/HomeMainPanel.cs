using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit
{
    public class HomeMainPanel : HomePanelBase
    {
        public Button fightBtn;
        public Button switchRoleBtn;
        public CharacterImage characterImage;
        public TMP_Text roleNameText;

        private void Start()
        {
            fightBtn.onClick.AddListener(() => { HomeHud.Inst.SwitchPanel(EHomePanel.FightPanel); });
            switchRoleBtn.onClick.AddListener(OnSwitchRole);
            characterImage.config.roleKey = $"Role_{ConfigMgr.LocalRoleSetting.selectRole}";
            roleNameText.text = characterImage.config.roleKey.ToLocalizeStr();
        }

        private void OnSwitchRole()
        {
            var setting = ConfigMgr.LocalRoleSetting;
            int roleId = setting.selectRole + 1;
            roleId %= setting.GetRoleCount();
            ConfigMgr.LocalRoleSetting.selectRole = roleId;
            LocalRoleSetting.Save();
            string roleKey = $"Role_{roleId}";
            // 切换角色
            characterImage.ChangeModelKey(roleKey);
            roleNameText.text = roleKey.ToLocalizeStr();
        }
    }
}