﻿using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

        private PlayerBuffs playerBuffs;


        private AssetPool textPool;
        private AssetPool buffCellPool;

        public List<BuffItem> buffItems;

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
            playerBuffs = BattleData.GetPlayerBuff();
            // 更新UI以显示buff
            RefreshUI();
        }

        public void RefreshUI()
        {
            ClearBuffItem(equippedBuffContainer);
            ClearBuffItem(unequippedBuffContainer);

            UpdateBuffTxet();

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

        private void UpdateBuffTxet()
        {
            var buffInfoList = playerBuffs.EquippedBuffs.GetBuffInfos().Combine();
            ShowBuffText(buffInfoList);
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
                ShowBuffText(item.buffs);
            }
            else
            {
                UpdateBuffTxet();
            }
        }

        private BuffItemCell InstantiateBuffItem(RectTransform container, BuffItem buffItem)
        {
            GameObject newBuffItem = buffCellPool.Get();
            newBuffItem.transform.SetParent(container, false);
            var buffItemCell = newBuffItem.GetComponent<BuffItemCell>();
            buffItemCell.TempItemCell = TempItemCell;
            buffItemCell.SetValue(buffItem);

            //刷新时清空监听
            buffItemCell.OnButtonClick = null;
            buffItemCell.OnButtonClick += OnBuffClick;
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