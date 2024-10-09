using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XiaoCao
{
    public class InventorySlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Image itemIcon;
        public Text itemNameText;
        public Text itemCountText;

        private DetailsPopupUI currentDetailsPopup; // 当前显示的浮窗

        public void UpdateSlot(Item item)
        {
            if (item != null)
            {
                itemIcon.sprite = GetItemIcon(item);
                itemNameText.text = item.Key;
                itemCountText.text = "x" + item.count.ToString();
            }
            else
            {
                itemIcon.sprite = null;
                itemNameText.text = "";
                itemCountText.text = "";
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 按住时显示详细信息浮窗
            ShowDetailsPopup();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // 松手时隐藏详细信息浮窗
            HideDetailsPopup();
        }


        private void ShowDetailsPopup()
        {
            if (itemIcon.sprite != null && currentDetailsPopup == null)
            {
                // 获取浮窗实例并显示详细信息
                currentDetailsPopup = DetailsPopupUI.GetPop();
                currentDetailsPopup.UpdateDetails(itemIcon.sprite, itemNameText.text, itemCountText.text);
            }
        }

        private void HideDetailsPopup()
        {
            if (currentDetailsPopup != null)
            {
                // 隐藏详细信息浮窗
                DetailsPopupUI.ClosePopup(currentDetailsPopup.gameObject);
                currentDetailsPopup = null;
            }
        }

        // 获取物品图标
        private Sprite GetItemIcon(Item item)
        {
            string iconPath = $"{item.type}/{item.Key}";
            Sprite icon = Resources.Load<Sprite>(iconPath);

            if (icon == null)
            {
                Debug.LogError("Icon not found at path: " + iconPath);
            }
            return icon;
        }
    }

}
