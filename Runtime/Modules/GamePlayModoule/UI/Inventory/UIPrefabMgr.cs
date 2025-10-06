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

        //暂无
        public GameObject popupUIPrefab;

        public GameObject itemCellPrefab;

        public ColorSettingSo hpBarColorSettingSo;

        public SimpleImageTween btnGlowEffect;

        public GameObject btnRedDot;

        private AssetPool popupUIPool;

        //暂未使用
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
