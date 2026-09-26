using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;

namespace XiaoCao
{
    /// <summary>
    /// 物品详情弹窗，负责根据物品配置显示名称、描述和获取途径。
    /// </summary>
    public class RelicShowDetail : MonoBehaviour
    {
        public TMP_Text nameText;
        public TMP_Text qualityText;

        public Transform attrTextContainer;
        public Transform attrNumTextContainer;
        private AttrNameNumGroupUI _attrNameNumGroupUI;

        public ItemCell itemCell;
        public Button equipBtn;
        public Button backBtn;
        public Button hideBtn;
        public ControlUISizeByCellCount controlUISize;

        /// <summary>
        /// 当前正在显示的物品。
        /// </summary>
        public Item CurrentItem { get; private set; }

        private Action<Item> _equipAction;

        private void Awake()
        {
            BindEvent();
            _attrNameNumGroupUI = new AttrNameNumGroupUI(attrTextContainer, attrNumTextContainer);
        }

        /// <summary>
        /// 显示指定物品的详情。
        /// </summary>
        /// <param name="item">需要显示的物品。</param>
        public void Show(Item item)
        {
            gameObject.SetActive(true);
            SetItem(item);
            controlUISize?.UpdateSize();
        }

        public void BindEvent()
        {
            equipBtn.onClick.AddListener(OnEquip);
            backBtn.onClick.AddListener(Hide);
            hideBtn.onClick.AddListener(Hide);
        }

        public void SetEquipAction(Action<Item> action)
        {
            _equipAction = action;
        }


        private void OnEquip()
        {
            _equipAction?.Invoke(CurrentItem);
            Hide();
        }

        /// <summary>
        /// 根据物品 id 查询 ItemInfo 配置并刷新详情内容。
        /// </summary>
        /// <param name="item">需要显示的物品。</param>
        public void SetItem(Item item)
        {
            CurrentItem = item;
            if (item == null)
            {
                Clear();
                return;
            }

            var relicItem = item.ToRelicItem();

            //显示名字 显示品级
            nameText.text = relicItem.LocalizeName;
            nameText.color = relicItem.GetColor();
            qualityText.text = $"{"QualityName".ToLocalizeStr()}: {(item.quality).ToLocalizedName()}";

            var valueDic = RelicValueCalc.GetRelicValue(item);
            _attrNameNumGroupUI.ShowAttrUI(valueDic.Keys.ToList(), valueDic.Values.ToList());
            RefreshItemCell(item);
        }

        /// <summary>
        /// 隐藏详情弹窗并清理当前显示内容。
        /// </summary>
        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 清理详情文本和可选的物品格显示。
        /// </summary>
        private void Clear()
        {
            CurrentItem = null;
            _equipAction = null;
        }

        /// <summary>
        /// 同步详情中的物品格；物品格未绑定时仅显示文字信息。
        /// </summary>
        private void RefreshItemCell(Item item)
        {
            if (itemCell == null)
            {
                return;
            }

            itemCell.gameObject.SetActive(true);
            itemCell.SetItem(item, UIItemTextType.NoNum);
            if (itemCell.icon == null)
            {
                return;
            }

            itemCell.icon.sprite = item.GetItemSprite();
            itemCell.icon.enabled = itemCell.icon.sprite != null;
        }
    }

    public class AttrNameNumGroupUI
    {
        public AttrNameNumGroupUI(Transform nameTextContainer, Transform numTextContainer)
        {
            this.nameTextContainer = nameTextContainer;
            this.numTextContainer = numTextContainer;
        }

        public Transform nameTextContainer;
        public Transform numTextContainer;

        /// <summary>
        /// 按属性列表刷新名称和值文本，并根据属性数量管理文本对象。
        /// </summary>
        /// <param name="attrList">需要显示的属性列表。</param>
        /// <param name="valueList">与属性列表对应的属性值列表。</param>
        public void ShowAttrUI(List<EAttr> attrList, List<float> valueList)
        {
            int showCount = attrList.Count;
            var nameTexts = GetTextList(nameTextContainer, showCount);
            var numTexts = GetTextList(numTextContainer, showCount);

            for (int i = 0; i < showCount; i++)
            {
                if (i < nameTexts.Length)
                {
                    nameTexts[i].text = attrList[i].ToLocalizedName();
                }

                if (i < numTexts.Length)
                {
                    float value = valueList[i];
                    numTexts[i].text = attrList[i].GetEAttrNumString(value);
                }
            }
        }

        /// <summary>
        /// 获取容器中的 TMP 文本，不足时复制首个文本，多余文本隐藏。
        /// </summary>
        private TMP_Text[] GetTextList(Transform container, int showCount)
        {
            if (container == null)
            {
                return Array.Empty<TMP_Text>();
            }

            TMP_Text[] texts = container.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length == 0)
            {
                if (showCount > 0)
                {
                    Debug.LogError($"{container.name} 缺少用于复制的 TMP_Text 模板");
                }

                return texts;
            }

            if (texts.Length < showCount)
            {
                TMP_Text template = texts[0];
                for (int i = texts.Length; i < showCount; i++)
                {
                    TMP_Text newText = UnityEngine.Object.Instantiate(template, container);
                    newText.gameObject.SetActive(true);
                }

                texts = container.GetComponentsInChildren<TMP_Text>(true);
            }

            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].gameObject.SetActive(i < showCount);
            }

            return texts;
        }
    }
}