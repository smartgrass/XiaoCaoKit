using System;
using System.Collections.Generic;
using cfg;
using EasyUI.Toast;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCao.UI;
using XiaoCaoKit;
using XiaoCaoKit.Runtime.Demo.Item;

namespace XiaoCao.UI
{
    public class SkillDetailUI : MonoBehaviour
    {
        //标题
        public TMP_Text titleText;

        //描述
        public TMP_Text desText;

        //升级按钮
        public Button upgradeBtn;

        public Button backBtn;

        public Transform costItemParent;

        public GameObject costShow;

        public string skillId;

        public PlayerSaveData PlayerSaveData => GameAllData.playerSaveData;


        private void Awake()
        {
            upgradeBtn.onClick.AddListener(OnUpgradeBtnClicked);
            backBtn.onClick.AddListener(OnHide);
        }

        public void Show(string setSkillId)
        {
            gameObject.SetActive(true);
            this.skillId = setSkillId;
            UpdateUI();
        }

        private void UpdateUI()
        {
            titleText.text = $"{LocalizeKey.GetSkillNameKey(skillId).ToLocalizeStr()}";
            desText.text = GetDesStr();
            UpdateCost();
        }


        public void OnUpgradeBtnClicked()
        {
            //技能升级
            int nextLv = PlayerSaveData.GetSkillLevel(skillId) + 1;
            //消耗材料
            List<Item> list = LubanTables.GetSkillUpgradeItems(skillId, nextLv);

            bool isEnough = PlayerSaveData.inventory.CheckEnoughItemList(list, true);
            if (!isEnough)
            {
                //提示
                // UITool.ShowTip(LocalizeKey.NotEnoughItem.ToLocalizeStr());
                Toast.Show("str", 1);
                return;
            }

            PlayerSaveData.AddSkillLevel(skillId);
            if (nextLv == 1)
            {
                //自动装备   
                SkillView.EquipSkill(skillId);
            }

            UpdateUI();
            HomeHud.EventSystem.SendEvent(HomeHudEventNames.SkillLevelChange);
            PlayerSaveData.SavaData();
        }

        void UpdateCost()
        {
            int curLv = PlayerSaveData.GetSkillLevel(skillId);
            if (curLv >= PlayerHelper.GetSkillMaxLevel(skillId))
            {
                //已满级
                costShow.SetActive(false);
            }
            else
            {
                costShow.SetActive(true);
                int nextLv = PlayerSaveData.GetSkillLevel(skillId) + 1;
                //消耗材料
                List<Item> list = LubanTables.GetSkillUpgradeItems(skillId, nextLv);
                UITool.SetCellListCount(costItemParent, list.Count);
                var cells = costItemParent.GetComponentsInChildren<ItemCell>(false);
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    var cell = cells[i];
                    cell.SetItem(item, UIItemTextType.NeedNum);
                }
            }
        }

        private string GetDesStr()
        {
            //等级: 1/5
            //等级伤害加成: 0%
            //技能描述:
            //向前突刺后连续斩击
            int lv = PlayerSaveData.GetSkillLevel(skillId);
            var skillUpgradeSetting = LubanTables.GetSkillUpgradeSetting(skillId);
            int maxLv = skillUpgradeSetting.MaxLevel;
            float levelDamageFactor = skillUpgradeSetting.LevelDamageFactor * Mathf.Max(0, lv - 1);

            string lvLine = $"{LocalizeKey.Lv.ToLocalizeStr()}: {lv}/{maxLv}";
            string damageLine = $"{LocalizeKey.LevelDamageFactor.ToLocalizeStr()}: {levelDamageFactor:P0}";
            // string onceDamageLine = $"{LocalizeKey.OnceDamage.ToLocalizeStr()}: {skillUpgradeSetting.Cd}";
            string needLvLine = $"{LocalizeKey.NeedLv.ToLocalizeStr()}: {skillUpgradeSetting.NeedLv}";
            string des = LocalizeKey.GetSkillDesc(skillId, lv);
            string desLine = $"{LocalizeKey.SkillDesc.ToLocalizeStr()}:{des}";

            return $"{lvLine}\n{needLvLine}\n{damageLine}\n{desLine}";
        }


        public void OnHide()
        {
            gameObject.SetActive(false);
        }
    }
}