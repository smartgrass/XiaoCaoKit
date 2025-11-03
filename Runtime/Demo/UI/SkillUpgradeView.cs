using cfg;
using EasyUI.Helpers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XiaoCao.UI;

namespace XiaoCao
{

    public class SkillUpgradeView: MonoBehaviour{
		public TMP_Text nameText;
		public TMP_Text desText;
		public Transform itemsParent;
        public Button sureBtn;

		private List<GameObject> itemCells = new List<GameObject>();
        private AssetPool itemCellPool;
		private bool isInit;
        private string skillId;
        public SkillPanel SkillPanel { get; set; }


        public void Init()
        {
            if (isInit)
            {
                return;
            }
            isInit = true;
            itemCellPool = new AssetPool(UIPrefabSo.Inst.itemCellPrefab);
            sureBtn.onClick.AddListener(OnUpgrade);
        }

        private void OnUpgrade()
        {
            //判断材料是否足够
            //足够则扣除
            var inventory = GameAllData.playerSaveData.inventory;
            List<Item> list = LubanTables.GetSkillUpgradeItems(skillId);
            bool isEnough = true;
            foreach (Item item in list)
            {
                if (!inventory.CheckEnoughItem(item.Key,item.num))
                {
                    isEnough = false;
                }
            }

            if (isEnough)
            {
                foreach (Item item in list)
                {
                    inventory.ConsumeItem(item.Key, item.num);
                }
                GameAllData.playerSaveData.AddSkillLevel(skillId);
                PlayerSaveData.SavaData();
                SkillPanel.UpdateUI();
            }
            else
            {
                //UIPrefabMgr.
                UIMgr.PopToast(LocalizeKey.NoEnough.ToLocalizeStr());
            }

        }

        public void ShowSkill(string skillId){
			Init();
            this.skillId = skillId;
            gameObject.SetActive(true);

            List<Item> list = LubanTables.GetSkillUpgradeItems(skillId);
            foreach (var item in list)
			{
				var cellGo = itemCellPool.Get();
				ItemCell cell = cellGo.GetComponent<ItemCell>();
				cell.SetItem(item, UIItemTextType.NeedNum);
            }
		}

		void Update()
		{
            if (Input.GetMouseButtonDown(0)) // 假设我们检测鼠标左键点击
            {
                Vector2 mousePosition = Input.mousePosition;
                if (!UIClickDetector.IsPointerOverTarget(mousePosition,(transform as RectTransform)))
                {
                    Hide();
                }
            }
        }


        public void Hide()
		{
            gameObject.SetActive(false);
		}

	}


    public static class UIClickDetector
    {
        public static bool IsPointerOverTarget(Vector2 mousePosition, RectTransform rect)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, mousePosition);
        }
    }

}