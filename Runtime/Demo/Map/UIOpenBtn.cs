﻿using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class UIOpenBtn : MonoBehaviour  
    {
        public bool IsHideSelf;

        public UIPanelType type;

        private void Start()
        {
            transform.GetComponent<Button>().onClick.AddListener(Open);
        }

        private void Open()
        {
            UIMgr.Inst.ShowView(type);
        }
    }
}