using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCao.UI
{
    public class HomeMainPanel : HomePanelBase
    {
        public Button fightBtn;
        public Button skillBtn;
        public Button switchRoleBtn;
        public CharacterImage characterImage;
        public TMP_Text roleNameText;
        public GameObject[] subViews;

        private void Start()
        {
            fightBtn.onClick.AddListener(() => { HomeHud.Inst.SwitchPanel(EHomePanel.FightPanel); });
            skillBtn.onClick.AddListener(() => { ShowSubView(1); });
            switchRoleBtn.onClick.AddListener(OnSwitchRole);
            characterImage.config.roleKey = $"Role_{ConfigMgr.Inst.LocalRoleSetting.selectRole}";
            roleNameText.text = characterImage.config.roleKey.ToLocalizeStr();

            //sub显示第一个,其余先隐藏
            ShowSubView(0);

            UICanvasMgr.Inst.EventSystem.AddEventListener<EHomeSubView>(UIEventNames.SwitchSubView,
                (data) => { ShowSubView((int)data); });

            // ShowSubView(data);
        }

        private void OnSwitchRole()
        {
            var setting = ConfigMgr.Inst.LocalRoleSetting;
            int roleId = setting.selectRole + 1;
            roleId %= setting.GetRoleCount();
            ConfigMgr.Inst.LocalRoleSetting.selectRole = roleId;
            LocalRoleSetting.Save();
            string roleKey = $"Role_{roleId}";
            // 切换角色
            characterImage.ChangeModelKey(roleKey);
            roleNameText.text = roleKey.ToLocalizeStr();
        }

        void ShowSubView(int index)
        {
            Debug.Log($"-- ShowSubView {index}");
            for (int i = 0; i < subViews.Length; i++)
            {
                subViews[i].SetActive(i == index);
            }
        }
    }

    public enum EHomeSubView
    {
        Main = 0,
        Skill = 1,
    }
}