using OdinSerializer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;
using XiaoCaoKit;
using static XiaoCao.BuffControl;

namespace XiaoCao
{
    public class BuffPanelView : MonoBehaviour
    {
        public Transform buffContainer; // 已装备buff的容器
        public Transform textContainer;

        [XCHeader("buff描述")] public Button switchBtn;
        public TextMeshProUGUI buffTitle;

        public Color exBuffTextColor = new Color32(160, 255, 160, 255);
        public Color norBuffTextColor = Color.white;
        List<BuffItemCell> cellList = new List<BuffItemCell>();
        List<TextMeshProUGUI> textList = new List<TextMeshProUGUI>();

        private PlayerBuffs playerBuffs;

        public void Init()
        {
            playerBuffs = PlayerHelper.GetPlayerBuffControl().playerBuffs;
            switchBtn.onClick.AddListener(OnSwitchBtn);
            // 更新UI以显示buff
            RefreshUI();
        }

        public void RefreshUI()
        {
            int count = playerBuffs.EquippedExBuffs.Count;
            cellList.Clear();
            UITool.SetCellListCount(buffContainer, count);
            cellList.AddRange(buffContainer.GetComponentsInChildren<BuffItemCell>(false));
            for (int i = 0; i < count; i++)
            {
                var item = playerBuffs.EquippedExBuffs[i];
                var cell = cellList[i];
                cell.Index = i;
                cell.IsEquiped = true;
                SetBuffCellInfo(cell, item);
            }

            UpdateBuffsTxet();
        }


        //显示总效果
        private void UpdateBuffsTxet()
        {
            var buffInfoList = playerBuffs.EquippedExBuffs.GetBuffInfos().Combine();
            Debug.Log($"--- count {buffInfoList.Count} {playerBuffs.EquippedExBuffs.GetBuffInfos().Count}");
            ShowBuffText(buffInfoList, playerBuffs.norBuff.GetBuffs.Combine());
        }

        private void ShowBuffText(List<BuffInfo> buffInfoList, List<BuffInfo> passiveBuffList = null)
        {
            textList.Clear();
            UITool.SetCellListCount(buffContainer, buffInfoList.Count + passiveBuffList.Count);
            textList.AddRange(buffContainer.GetComponentsInChildren<TextMeshProUGUI>(false));

            for (int i = 0; i < buffInfoList.Count; i++)
            {
                var text = textList[i];
                text.text = LocalizeKey.GetBuffInfoDesc(buffInfoList[i]);
                text.color = exBuffTextColor;
            }

            int offset = buffInfoList.Count;

            for (int i = 0; i < buffInfoList.Count; i++)
            {
                var text = textList[offset + i];
                text.text = LocalizeKey.GetBuffInfoDesc(passiveBuffList[i]);
                text.color = exBuffTextColor;
            }
        }

        private void OnBuffClick(BuffItem item)
        {
            //显示单个buff
            if (item.GetBuffType != EBuffType.None)
            {
                buffTitle.text = $"{LocalizeKey.BuffEffect.ToLocalizeStr()} lv{item.level + 1}";
                switchBtn.gameObject.SetActive(true);
                ShowBuffText(item.buffs);
            }
            else
            {
                UpdateBuffsTxet();
            }
        }

        private void OnBuffChange()
        {
            RefreshUI();
        }


        private int GetEquippedBuffsLevel()
        {
            int level = 0;
            foreach (var item in playerBuffs.EquippedExBuffs)
            {
                if (item.IsEnable)
                {
                    level += item.level + 1;
                }
            }

            return level;
        }


        private void OnSwitchBtn()
        {
            UpdateBuffsTxet();
        }

        private void SetBuffCellInfo(BuffItemCell buffItemCell,BuffItem buffItem)
        {
            buffItemCell.panelView = this;
            buffItemCell.SetValue(buffItem);

            //刷新时清空监听
            buffItemCell.OnButtonClick = null;
            buffItemCell.OnButtonClick += OnBuffClick;
            buffItemCell.OnBuffChangeAct = null;
            buffItemCell.OnBuffChangeAct += OnBuffChange;
        }
    }
}