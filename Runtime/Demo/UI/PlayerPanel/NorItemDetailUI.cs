using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    /// <summary>
    /// 普通物品详情界面，负责显示物品名称、描述、获取途径和物品格。
    /// </summary>
    public class NorItemDetailUI : MonoBehaviour
    {
        public TMP_Text nameText;
        public TMP_Text descText;
        public TMP_Text getWayText;
        public ItemCell itemCell;
        public Button backBtn;
        public ControlUISizeByText controlUISize;

        /// <summary>
        /// 刷新普通物品的名称、描述、获取途径和物品格，并按文本内容重新适配背景高度。
        /// </summary>
        public void SetItem(Item item)
        {
            nameText.text = item.GetItemName();
            descText.text = LocalizeKey.GetItemDesc(item);
            getWayText.text = LocalizeKey.GetItemObtainWay(item);
            itemCell.SetItem(item, UIItemTextType.NoNum);
            controlUISize.Refresh();
        }
    }
}
