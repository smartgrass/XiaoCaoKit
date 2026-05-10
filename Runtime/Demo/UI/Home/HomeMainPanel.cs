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
        public Button growBtn;//打开提升角色面板
        public NorBtn switchRoleBtn;
        public CharacterImage characterImage;
        public GameObject[] subViews;
        private int _currentSubViewIndex;

        private void Start()
        {
            fightBtn.onClick.AddListener(() => { HomeHud.Inst.SwitchPanel(EHomePanel.FightPanel); });
            growBtn.onClick.AddListener(() => { ShowSubView(1); });
            switchRoleBtn.btn.onClick.AddListener(OnSwitchRole);
            characterImage.config.roleKey = $"Role_{ConfigMgr.Inst.LocalRoleSetting.selectRole}";
            switchRoleBtn.titleText.text = characterImage.config.roleKey.ToLocalizeStr();

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
            switchRoleBtn.titleText.text = roleKey.ToLocalizeStr();
        }

        void ShowSubView(int index)
        {
            Debug.Log($"-- ShowSubView {index}");
            _currentSubViewIndex = index;
            for (int i = 0; i < subViews.Length; i++)
            {
                subViews[i].SetActive(i == index);
            }
        }

        /// <summary>
        /// 切换 Home 主界面的子视图。
        /// </summary>
        public void ShowSubView(EHomeSubView subView)
        {
            ShowSubView((int)subView);
        }

        /// <summary>
        /// 判断指定子视图当前是否处于显示状态。
        /// </summary>
        public bool IsSubViewActive(EHomeSubView subView)
        {
            int index = (int)subView;
            return index >= 0 &&
                   index < subViews.Length &&
                   subViews[index] != null &&
                   subViews[index].activeSelf;
        }

        /// <summary>
        /// 获取当前激活的子视图枚举。
        /// </summary>
        public EHomeSubView GetCurrentSubView()
        {
            return (EHomeSubView)_currentSubViewIndex;
        }
    }

    public enum EHomeSubView
    {
        Main = 0,
        Skill = 1,
    }
}
