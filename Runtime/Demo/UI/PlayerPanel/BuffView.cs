using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCaoKit;

// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace XiaoCao.UI
{
    public class BuffView : EnableShowUI
    {
        public Transform buffContainer; // 已装备buff的容器
        public Transform textContainer;

        [XCHeader("buff描述")] public Button switchBtn;
        public TextMeshProUGUI buffTitle;

        public Color exBuffTextColor = new Color32(160, 255, 160, 255);
        public Color norBuffTextColor = Color.white;
        private List<BuffItemCell> _cellList = new List<BuffItemCell>();
        private List<TextMeshProUGUI> _textList = new List<TextMeshProUGUI>();

        public int baseCount = 30;

        private BuffItem tempBuffItem;
        private PlayerBuffs playerBuffs;

        public override void OnEnable()
        {
            var control = PlayerHelper.GetPlayerBuffControl();
            playerBuffs = control != null ? playerBuffs : null;
            switchBtn.onClick.RemoveListener(OnSwitchBtn);
            switchBtn.onClick.AddListener(OnSwitchBtn);
            // 更新UI以显示buff
            UpdateUI();
        }

        public override void UpdateUI()
        {
            if (tempBuffItem == null)
            {
                switchBtn.gameObject.SetActive(false);
            }

            if (playerBuffs == null)
            {
                playerBuffs = new PlayerBuffs();
            }

            int norBuffCount = playerBuffs.norBuff.IsEnable ? 1 : 0;
            List<BuffItem> buffList = new List<BuffItem>();
            if (norBuffCount > 0)
            {
                buffList.Add(playerBuffs.norBuff);
            }

            buffList.AddRange(playerBuffs.EquippedExBuffs);
            int hasCount = buffList.Count;
            int showCount = Math.Max(baseCount, hasCount);
            _cellList.Clear();
            UITool.SetCellListCount(buffContainer, showCount);
            _cellList.AddRange(buffContainer.GetComponentsInChildren<BuffItemCell>(false));

            if (_cellList.Count < showCount)
            {
                Debug.LogError($"Buff容器配置错误，BuffItemCell数量不足：{_cellList.Count}/{showCount}");
                return;
            }

            for (int i = 0; i < showCount; i++)
            {
                var cell = _cellList[i];
                if (i < hasCount)
                {
                    var item = buffList[i];

                    cell.Index = i;
                    SetBuffCellInfo(cell, item);
                }
                else
                {
                    BuffItem emptyItem = new BuffItem();
                    SetBuffCellInfo(cell, emptyItem);
                }

                cell.gameObject.SetActive(true);
            }

            UpdateBuffsText();
        }


        //显示总效果
        private void UpdateBuffsText()
        {
            var buffInfoList = playerBuffs.EquippedExBuffs.GetBuffInfos().Combine();
            Debug.Log($"--- count {buffInfoList.Count} {playerBuffs.EquippedExBuffs.GetBuffInfos().Count}");
            ShowBuffText(buffInfoList, playerBuffs.norBuff.GetBuffs.Combine());
            buffTitle.text = $"{LocalizeKey.AllBuffEffect.ToLocalizeStr()}";
        }

        private void ShowBuffText(List<BuffInfo> buffInfoList, List<BuffInfo> passiveBuffList = null)
        {
            _textList.Clear();
            if (passiveBuffList == null)
            {
                passiveBuffList = new List<BuffInfo>();
            }

            if (buffInfoList == null)
            {
                buffInfoList = new List<BuffInfo>();
            }

            int textCount = buffInfoList.Count + passiveBuffList.Count;
            UITool.SetCellListCount(textContainer, textCount);
            _textList.AddRange(textContainer.GetComponentsInChildren<TextMeshProUGUI>(false));

            if (_textList.Count < textCount)
            {
                Debug.LogError($"Buff文本容器配置错误，TextMeshProUGUI数量不足：{_textList.Count}/{textCount}");
                return;
            }

            for (int i = 0; i < buffInfoList.Count; i++)
            {
                var text = _textList[i];
                text.text = LocalizeKey.GetBuffInfoDesc(buffInfoList[i]);
                text.color = exBuffTextColor;
            }

            int offset = buffInfoList.Count;

            for (int i = 0; i < passiveBuffList.Count; i++)
            {
                var text = _textList[offset + i];
                text.text = LocalizeKey.GetBuffInfoDesc(passiveBuffList[i]);
                text.color = norBuffTextColor;
            }
        }

        private void OnBuffClick(BuffItem item)
        {
            Debug.Log($"-- OnBuffClick {item} {item.GetBuffType}");
            //显示单个buff
            if (item.GetBuffType != EBuffType.None)
            {
                // buffTitle.text = $"{LocalizeKey.BuffEffect.ToLocalizeStr()} lv{item.level + 1}";
                buffTitle.text = $"{LocalizeKey.GetBuffNameKey(item.GetFirstEBuff).ToLocalizeStr()} lv{item.level + 1}";
                switchBtn.gameObject.SetActive(true);
                tempBuffItem = item;
                if (item.GetBuffType == EBuffType.Nor)
                {
                    ShowBuffText(null, item.buffs);
                }
                else
                {
                    ShowBuffText(item.buffs);
                }
            }
            else
            {
                UpdateBuffsText();
                tempBuffItem = null;
                switchBtn.gameObject.SetActive(false);
            }
        }

        private void OnBuffChange()
        {
            UpdateUI();
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
            UpdateBuffsText();
        }

        private void SetBuffCellInfo(BuffItemCell buffItemCell, BuffItem buffItem)
        {
            buffItemCell.view = this;
            buffItemCell.SetValue(buffItem);

            //刷新时清空监听
            buffItemCell.onButtonClick = null;
            buffItemCell.onButtonClick += OnBuffClick;
            buffItemCell.onBuffChangeAct = null;
            buffItemCell.onBuffChangeAct += OnBuffChange;
        }
    }
}