using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XiaoCao
{
    public class ItemCell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Item Item { get; private set; }

        public Image icon;

        public Button btn;

        public TextMeshProUGUI text;


        public void SetItem(Item item, UIItemTextType textType = UIItemTextType.Default)
        {
            this.Item = item;

            if (textType == UIItemTextType.NeedNum)
            {
                int hasCount = GameAllData.playerSaveData.inventory.GetItemCount(item.ItemKey);
                text.text = $"{hasCount}/{item.num}";
            }
            else
            {
                text.text = item.num.ToString();
            }
        }

        public void SetNum(int num)
        {
            text.text = num.ToString();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }
    }

    public enum UIItemTextType
    {
        Default,
        NeedNum
    }
}