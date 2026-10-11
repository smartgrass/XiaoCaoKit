using System.Collections.Generic;
using EasyUI.Toast;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using cfg;
using XiaoCaoKit;
using XiaoCaoKit.UI;

namespace XiaoCao.UI
{
    public class RoleTabView : TabView
    {
        public Transform attrTextParent;
        public GameObject unFightUI;
        public Transform roleList;

        private PlayerSaveData PlayerSaveData => GameAllData.playerSaveData;

        private void Awake()
        {
            UICanvasMgr.Inst.EventSystem.AddEventListener(UIEventNames.RoleLevelChange, OnRoleLevelChange);
            UICanvasMgr.Inst.EventSystem.AddEventListener(UIEventNames.BlessingChange, OnRoleLevelChange);
        }

        private void OnDestroy()
        {
            UICanvasMgr.Inst.EventSystem.RemoveEventListener(UIEventNames.RoleLevelChange, OnRoleLevelChange);
            UICanvasMgr.Inst.EventSystem.RemoveEventListener(UIEventNames.BlessingChange, OnRoleLevelChange);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            UpdateUI();
        }


        /// <summary>
        /// 刷新角色页显示，供新手引导主动重绘。
        /// </summary>
        public void RefreshGuideUI()
        {
            UpdateUI();
        }

        private void OnUpgradeBtnClick()
        {
            if (!ResMgr.IsLoadBaseFinish)
            {
                return;
            }


            int nextLv = PlayerSaveData.lv + 1;
            if (!IsEnoughCost(nextLv, true))
            {
                Debug.LogError($"-- no IsEnoughCost");
                return;
            }

            //等级提升统一走 PlayerSaveData, 内部会广播升级事件弹出升级提示
            PlayerSaveData.UpgradeLevel();
            UICanvasMgr.Inst.EventSystem.SendEvent(UIEventNames.RoleLevelChange);
        }

        /// <summary>
        /// 响应角色等级变化并刷新角色面板。
        /// </summary>
        private void OnRoleLevelChange()
        {
            UpdateUI();
        }

        private bool IsEnoughCost(int nextLv, bool isCost)
        {
            List<Item> costItems = LubanTables.GetRoleUpgradeItems(nextLv);
            if (costItems == null || costItems.Count == 0)
            {
                return true;
            }

            bool isEnough = PlayerSaveData.inventory.CheckEnoughItemList(costItems, isCost);
            return isEnough;
        }


        public void UpdateUI()
        {
            if (!ResMgr.IsLoadBaseFinish)
            {
                return;
            }

            ShowPlayerAttr();

            //UnFight 属于 Home 场景的内容, 关卡内隐藏主控角色列表
            if (GameDataCommon.Current.gameStage == EGameStage.Home)
            {
                unFightUI.SetActive(true);
                RefreshRoleList();
            }
            else
            {
                unFightUI.SetActive(false);
            }
        }

        /// <summary>
        /// 刷新主控角色列表: 按解锁数量生成 RoleCell, 并把 LocalRoleSetting.selectRole 对应的格子置为选中。
        /// </summary>
        private void RefreshRoleList()
        {
            LocalRoleSetting roleSetting = ConfigMgr.Inst.LocalRoleSetting;
            int roleCount = roleSetting.GetRoleCount();

            UITool.SetCellListCount(roleList, roleCount);

            RoleCell[] cells = roleList.GetComponentsInChildren<RoleCell>(true);
            for (int i = 0; i < roleCount; i++)
            {
                cells[i].SetRole(i, i == roleSetting.selectRole, OnRoleCellClick);
            }
        }

        /// <summary>
        /// 点击角色格子后写入主控角色配置, 并刷新选中状态。
        /// </summary>
        private void OnRoleCellClick(int roleIndex)
        {
            ConfigMgr.Inst.LocalRoleSetting.selectRole = roleIndex;
            LocalRoleSetting.Save();
            RefreshRoleList();
        }

        private void ShowPlayerAttr()
        {
            UITool.SetCellListCount(attrTextParent, 7);

            var texts = attrTextParent.GetComponentsInChildren<TMP_Text>();
            RoleAttr attr = PlayerSaveData.GetInitPlayerAttr(false);

            texts[0].text = $"Lv {PlayerSaveData.lv}";
            texts[1].text = GetRoleExpShowText();
            texts[2].text = GetRoleAttrShowText(EAttr.MaxHp, attr);
            texts[3].text = GetRoleAttrShowText(EAttr.Atk, attr);
            texts[4].text = GetRoleAttrShowText(EAttr.Def, attr);
            texts[5].text = GetRoleAttrShowText(EAttr.Crit, attr);
            texts[6].text = GetRoleAttrShowText(EAttr.CritDamage, attr);
        }

        /// <summary>
        /// 格式化角色经验显示: 未满级显示 当前经验/升级所需经验, 满级后只显示累计经验。
        /// </summary>
        private string GetRoleExpShowText()
        {
            string expName = LocalizeKey.Exp.ToLocalizeStr();
            if (PlayerSaveData.lv >= GameSetting.MaxRoleLevel)
            {
                return $"{expName}:{PlayerSaveData.exp}";
            }

            int nextLevelExp = CharacterAttrCalc.GetRoleNextLevelExp(PlayerSaveData.lv);
            return $"{expName}:{PlayerSaveData.exp}/{nextLevelExp}";
        }

        /// <summary>
        /// 格式化角色属性显示值，统一按四舍五入显示整数。
        /// </summary>
        private string GetRoleAttrShowText(EAttr attr, RoleAttr roleAttr)
        {
            if (attr == EAttr.MaxHp)
            {
                return
                    $"{attr.ToLocalizedName()}:{attr.GetEAttrNumString(roleAttr.hp)}/{attr.GetEAttrNumString(roleAttr.MaxHp)}";
            }

            return $"{attr.ToLocalizedName()}:{attr.GetEAttrNumString(roleAttr.GetAttribute(attr).CurrentValue)}";
        }
    }
}
