using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <see cref="HomeMainPanel"/>
    /// <see cref="HomeFightPanel"/>
    public class HomeHud : MonoBehaviour, ICanvasMgr
    {
        private const string GuideRootPrefabPath = "Assets/_Res/UI/HomeUI/HomeGuideRoot.prefab";

        public static HomeHud Inst;
        public Canvas canvas;
        public Canvas Canvas => canvas;
        public HomeGuideController GuideController => _guideController;
        private HomeGuideController _guideController;

        private void Awake()
        {
            Inst = this;
            UICanvasMgr.Inst.canvasMgr = this;
            canvas = transform.GetComponentInParent<Canvas>();
            GameDataCommon.Current.isFighting = false;
        }

        private IEnumerator Start()
        {
            while (!ResMgr.IsLoadBaseFinish)
            {
                yield return null;
            }

            EnsureGuideController();
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

        /// <summary>
        /// 判断指定主页面板当前是否处于显示状态。
        /// </summary>
        public bool IsPanelActive(EHomePanel panel)
        {
            return panel >= 0 &&
                   (int)panel < panels.Count &&
                   panels[(int)panel] != null &&
                   panels[(int)panel].activeSelf;
        }

        /// <summary>
        /// 供调试入口确保 HomeGuideRoot 已实例化，并返回当前引导控制器。
        /// </summary>
        public HomeGuideController EnsureGuideControllerForDebug()
        {
            EnsureGuideController();
            return _guideController;
        }

        void EnsureGuideController()
        {
            if (canvas == null)
            {
                return;
            }

            _guideController = canvas.GetComponentInChildren<HomeGuideController>(true);
            if (_guideController != null)
            {
                _guideController.Initialize(this);
                return;
            }

            GameObject prefab = ResMgr.LoadPrefab(GuideRootPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"新手引导 prefab 不存在: {GuideRootPrefabPath}");
                return;
            }

            GameObject rootGo = Instantiate(prefab, canvas.transform, false);
            rootGo.name = "HomeGuideRoot";
            _guideController = rootGo.GetComponent<HomeGuideController>();
            if (_guideController == null)
            {
                _guideController = rootGo.AddComponent<HomeGuideController>();
            }

            _guideController.Initialize(this);
        }
    }

    public enum EHomePanel
    {
        MainPanel,
        FightPanel,
    }
}
