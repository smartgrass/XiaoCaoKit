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


        /// <param name="isNeedItem">是否显示需求数量</param>
        public void SetItem(Item Item,bool isNeedItem =false)
        {
            this.Item = Item;

            if (isNeedItem)
            {
                int hasCount = GameAllData.playerSaveData.inventory.GetItemCount(Item.Key);
                text.text = $"{hasCount}/{Item.count}";
            }
            else
            {
                text.text = Item.count.ToString();
            }

        }

        public void OnPointerDown(PointerEventData eventData)
        {

        }

        public void OnPointerUp(PointerEventData eventData)
        {

        }
    }

}