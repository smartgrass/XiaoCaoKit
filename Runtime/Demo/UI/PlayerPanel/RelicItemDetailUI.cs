using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using XiaoCao.UI;

namespace XiaoCao
{
    /// <summary>
    /// 圣器详情界面，负责显示圣器名称、品级和属性列表。
    /// </summary>
    public class RelicItemDetailUI : MonoBehaviour
    {
        public TMP_Text nameText;
        public TMP_Text qualityText;

        public Transform attrTextContainer;
        public Transform attrNumTextContainer;

        public ItemCell itemCell;
        public ControlUISizeByCellCount controlUISize;

        private AttrNameNumGroupUI _attrNameNumGroupUI;

        private void Awake()
        {
            _attrNameNumGroupUI = new AttrNameNumGroupUI(attrTextContainer, attrNumTextContainer);
        }

        /// <summary>
        /// 刷新圣器详情的名称、品级、属性列表和物品格，并按属性行数更新界面高度。
        /// </summary>
        public void SetItem(Item item)
        {
            RelicItem relicItem = item.ToRelicItem();
            nameText.text = relicItem.LocalizeName;
            nameText.color = relicItem.GetColor();
            qualityText.text = $"{"QualityName".ToLocalizeStr()}: {item.quality.ToLocalizedName()}";

            Dictionary<EAttr, float> valueDic = RelicValueCalc.GetRelicValue(item);
            _attrNameNumGroupUI.ShowAttrUI(valueDic.Keys.ToList(), valueDic.Values.ToList());

            itemCell.SetItem(item, UIItemTextType.NoNum);
            controlUISize.UpdateSize();
        }
    }
}
