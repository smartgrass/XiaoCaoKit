using System;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/UIPrefabSo", fileName = "UIPrefabSo")]
    public class UIPrefabSo : ScriptableObject
    {
        public static UIPrefabSo Inst;

        private void OnEnable()
        {
            Inst = this;
        }

        //暂无
        public GameObject popupUIPrefab;

        public GameObject itemCellPrefab;

        public ColorSettingSo hpBarColorSettingSo;

        public GameObject btnGlowEffect;

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