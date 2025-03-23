using OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;
using static XiaoCao.BuffControl;

namespace XiaoCao
{
    public class BuffPanelView : MonoBehaviour
    {
        public Transform prefabsTf;
        public RectTransform equippedBuffContainer; // 已装备buff的容器
        public RectTransform unequippedBuffContainer; // 未装备buff的容器

        public RectTransform textContainer;

        public GameObject buffItemPrefab; // BuffItem的Prefab
        public TextMeshProUGUI buffTextPrefab; // BuffItem的Prefab

        [XCHeader("buff描述")]
        public Button switchBtn;
        public TextMeshProUGUI buffTitle;

        private PlayerBuffs playerBuffs;


        private AssetPool textPool;
        private AssetPool buffCellPool;

        //ShareData
        public BuffItemCell TempItemCell { get; private set; }


        public void Init()
        {
            textPool = new AssetPool(buffTextPrefab.gameObject);
            buffCellPool = new AssetPool(buffItemPrefab.gameObject);

            buffTextPrefab.transform.SetParent(prefabsTf, false);
            buffItemPrefab.transform.SetParent(prefabsTf, false);

            TempItemCell = Instantiate(buffItemPrefab, transform).GetComponent<BuffItemCell>();
            TempItemCell.gameObject.SetActive(false);
            TempItemCell.enabled = false;
            TempItemCell.EnableRayCast(false);

            prefabsTf.gameObject.SetActive(false);
            playerBuffs = PlayerHelper.GetPlayerBuff().playerBuffs;
            switchBtn.onClick.AddListener(OnSwitchBtn);
            // 更新UI以显示buff
            RefreshUI();
        }

        public void RefreshUI()
        {
            //先清空
            ClearBuffItem(equippedBuffContainer);
            ClearBuffItem(unequippedBuffContainer);

            UpdateEquippedBuffsTxet();

            // 显示已装备的buff
            for (int i = 0; i < playerBuffs.MaxEquipped; i++)
            {
                BuffItem item = default;
                if (i < playerBuffs.EquippedBuffs.Count)
                {
                    item = playerBuffs.EquippedBuffs[i];
                }
                else
                {
                    item = new BuffItem();
                }
                var cell = InstantiateBuffItem(equippedBuffContainer, item);
                cell.Index = i;
                cell.IsEquiped = true;
            }
            // 显示未装备的buff
            for (int i = 0; i < playerBuffs.UnequippedBuffs.Count; i++)
            {
                var cell = InstantiateBuffItem(unequippedBuffContainer, playerBuffs.UnequippedBuffs[i]);
                cell.Index = i;
                cell.IsEquiped = false;
            }
        }

        private void ShowBuffText(List<BuffInfo> buffInfoList)
        {
            if (buffInfoList == null)
            {
                return;
            }

            ClearTexts();
            foreach (var buffInfo in buffInfoList)
            {
                GameObject newBuffItem = textPool.Get();
                newBuffItem.transform.SetParent(textContainer, false);
                var text = newBuffItem.GetComponent<TextMeshProUGUI>();
                text.text = LocalizeKey.GetBuffInfoDesc(buffInfo);
            }
        }

        private void OnBuffClick(BuffItem item)
        {
            //显示单个buff
            if (item.GetBuffType != EBuffType.None)
            {
                buffTitle.text = $"{LocalizeKey.BuffEffect.ToLocalizeStr()} lv{item.level+1}";
                switchBtn.gameObject.SetActive(true);
                ShowBuffText(item.buffs);
            }
            else
            {
                UpdateEquippedBuffsTxet();
            }
        }

        private void OnBuffChange()
        {
            RefreshUI();
        }

        //显示总效果
        private void UpdateEquippedBuffsTxet()
        {
            buffTitle.text = $"{LocalizeKey.EquippedBuffEffect.ToLocalizeStr()} lv{GetEquippedBuffsLevel()}";
            switchBtn.gameObject.SetActive(false);
            var buffInfoList = playerBuffs.EquippedBuffs.GetBuffInfos().Combine();
            Debug.Log($"--- count {buffInfoList.Count} {playerBuffs.EquippedBuffs.GetBuffInfos().Count}");
            ShowBuffText(buffInfoList);
        }

        private int GetEquippedBuffsLevel()
        {
            int level = 0;
            foreach (var item in playerBuffs.EquippedBuffs)
            {
                if (item.IsEnable)
                {
                    level += item.level+1;
                }
            }
            return level;
        }


        private void OnSwitchBtn()
        {
            UpdateEquippedBuffsTxet();
        }

        private BuffItemCell InstantiateBuffItem(RectTransform container, BuffItem buffItem)
        {
            GameObject newBuffItem = buffCellPool.Get();
            newBuffItem.transform.SetParent(container, false);
            var buffItemCell = newBuffItem.GetComponent<BuffItemCell>();
            buffItemCell.TempItemCell = TempItemCell;
            buffItemCell.panelView = this;
            buffItemCell.SetValue(buffItem);

            //刷新时清空监听
            buffItemCell.OnButtonClick = null;
            buffItemCell.OnButtonClick += OnBuffClick;
            buffItemCell.OnBuffChangeAct = null;
            buffItemCell.OnBuffChangeAct += OnBuffChange;
            return buffItemCell;
        }



        private void ClearTexts()
        {
            int len = textContainer.childCount;
            for (int i = len - 1; i >= 0; i--)
            {
                textPool.Release(textContainer.GetChild(i).gameObject);
            }
        }

        private void ClearBuffItem(Transform container)
        {
            int len = container.childCount;
            for (int i = len - 1; i >= 0; i--)
            {
                buffCellPool.Release(container.GetChild(i).gameObject);
            }
        }
    }
}