using UnityEngine;

namespace XiaoCao
{
    public class EquipRelicCell : MonoBehaviour
    {
        public ItemCell itemCell;

        public GameObject emptyShow;

        public int UIIndex { get; set; }//记录顺序
        
        public void ShowItem(Item item)
        {
            bool isShow = item.type == ItemType.None;
            emptyShow.SetActive(!isShow);
            itemCell.gameObject.SetActive(isShow);
            if (isShow)
            {
                itemCell.SetItem(item);
            }
        }
    }
}