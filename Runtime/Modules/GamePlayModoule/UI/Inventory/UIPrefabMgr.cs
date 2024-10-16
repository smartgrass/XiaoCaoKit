using UnityEngine;

namespace XiaoCao
{
    public class UIPrefabMgr : MonoBehaviour
    {
        public static UIPrefabMgr Inst;
        private void Awake()
        {
            Inst = this;
        }


        public GameObject popupUIPrefab;
        public GameObject itemCellPrefab;

        private AssetPool popupUIPool;

        public AssetPool PopupUIPool
        {
            get
            {
                if (popupUIPool == null)
                {
                    popupUIPool = new AssetPool(popupUIPrefab);
                }
                return popupUIPool;
            }
        }
    }
}
