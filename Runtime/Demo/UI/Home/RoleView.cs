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
    public class RoleView : MonoBehaviour
    {
        public Button upgradeBtn;
        public TMP_Text levelText;
        [Header("1满级")] public UIStateChange fullLvChange;
        public Transform attrTextParent;
        public Transform costParent;
        public GameObject unFightUI;

        private PlayerSaveData PlayerSaveData => GameAllData.playerSaveData;

        private void Awake()
        {
            upgradeBtn.onClick.AddListener(OnUpgradeBtnClick);
        }

        private void OnEnable()
        {
            UpdateUI();
        }

        private void OnUpgradeBtnClick()
        {
            if (!ResMgr.IsLoadBaseFinish)
            {
                return;
            }


            int nextLv = Mathf.Max(0, PlayerSaveData.lv) + 1;
            if (!IsEnoughCost(nextLv, true))
            {
                Debug.LogError($"-- no IsEnoughCost");
                return;
            }

            Toast.Show(LocalizeKey.LevelUp.ToLocalizeStr());
            PlayerSaveData.lv = nextLv;
            PlayerSaveData.SavaData();
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


        private void UpdateUI()
        {
            if (!ResMgr.IsLoadBaseFinish)
            {
                return;
            }

            ShowPlayerAttr();

            if (GameDataCommon.Current.isFighting)
            {
                unFightUI.SetActive(false);
            }
            else
            {
                ShowUpgradeUI();
                unFightUI.SetActive(true);
            }
        }

        private void ShowUpgradeUI()
        {
            if (PlayerSaveData.lv >= GameSetting.MaxRoleLevel)
            {
                //角色满级
                fullLvChange.SetState(1);
                return;
            }

            fullLvChange.SetState(0);
            UpdateCost();

            int nextLv = PlayerSaveData.lv + 1;
            if (!IsEnoughCost(nextLv, false))
            {
                //将升级按钮变灰
                upgradeBtn.interactable = false;
                return;
            }

            upgradeBtn.interactable = true;
        }

        private void ShowPlayerAttr()
        {
            UITool.SetCellListCount(attrTextParent, 5);

            var texts = attrTextParent.GetComponentsInChildren<TMP_Text>();
            PlayerAttr attr = PlayerSaveData.GetPlayerAttr();

            texts[0].text = $"Lv {PlayerSaveData.lv}";
            texts[1].text = GetRoleAttrShowText(EAttr.MaxHp, attr);
            texts[2].text = GetRoleAttrShowText(EAttr.Atk, attr);
            texts[3].text = GetRoleAttrShowText(EAttr.Def, attr);
            texts[4].text = GetRoleAttrShowText(EAttr.Crit, attr);
        }

        private void UpdateCost()
        {
            List<Item> list = LubanTables.GetRoleUpgradeItems(PlayerSaveData.lv + 1);
            UITool.SetCellListCount(costParent, list.Count);

            var cells = costParent.GetComponentsInChildren<ItemCell>(false);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var cell = cells[i];
                cell.SetItem(item, UIItemTextType.NeedNum);
            }
        }

        string GetRoleAttrShowText(EAttr attr, PlayerAttr playerAttr)
        {
            if (attr == EAttr.MaxHp)
            {
                return $"{"Hp".ToLocalizeStr()}:{playerAttr.hp}/{playerAttr.MaxHp}";
            }

            return $"{attr.ToString().ToLocalizeStr()}:{playerAttr.GetAttribute(attr).CurrentValue}";
        }
    }
}