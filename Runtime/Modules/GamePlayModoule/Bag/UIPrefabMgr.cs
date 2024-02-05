using UnityEngine;

namespace XiaoCao
{
    public class UIPrefabMgr : MonoSingletonPrefab<UIPrefabMgr>
    {
        public GameObject popupUIPrefab;
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
