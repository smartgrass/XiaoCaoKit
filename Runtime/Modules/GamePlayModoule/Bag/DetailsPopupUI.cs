using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class DetailsPopupUI : MonoBehaviour
    {
        public Image itemIcon;
        public Text itemNameText;
        public Text itemCountText;

        public void UpdateDetails(Sprite icon, string itemName, string itemCount)
        {
            itemIcon.sprite = icon;
            itemNameText.text = itemName;
            itemCountText.text = itemCount;
        }

        public static void ClosePopup(GameObject gameObject)
        {
            UIPrefabMgr.Inst.PopupUIPool.Release(gameObject);
        }

        public static DetailsPopupUI GetPop()
        {
            return UIPrefabMgr.Inst.PopupUIPool.Get().GetComponent<DetailsPopupUI>();
        }
    }
}
