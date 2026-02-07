using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <see cref="HomeMainPanel"/>
    /// <see cref="HomeFightPanel"/>
    public class HomeHud : MonoBehaviour, ICanvasMgr
    {
        public static HomeHud Inst;
        public Canvas canvas;
        public Canvas Canvas => canvas;

        private void Awake()
        {
            Inst = this;
            UICanvasMgr.Inst.canvasMgr = this;
            canvas = transform.GetComponentInParent<Canvas>();
            GameDataCommon.Current.isFighting = false;
        }

        private void OnDestroy()
        {
            UICanvasMgr.Inst.EventSystem.ClearAllEvents();
        }

        public List<GameObject> panels;


        public void SwitchPanel(EHomePanel panel)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].SetActive(false);
            }

            panels[(int)panel].SetActive(true);
        }

        public void BackToMain()
        {
            SwitchPanel(EHomePanel.MainPanel);
        }
    }

    public enum EHomePanel
    {
        MainPanel,
        FightPanel,
    }
}