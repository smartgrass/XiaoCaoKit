using System;
using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCaoKit;
using XiaoCaoKit.UI;

namespace XiaoCao.UI
{
    public class LevelBtn : NorBtn
    {
        public UIStateChange stateChange;
        public Transform outlineFlow;
        public Transform outline;
        public Transform starsRoot;
        public Sprite starOnSprite;
        public Sprite starOffSprite;
        public Image[] starImages;
        public GameObject selectShowObject;
        public GameObject selectHideObject;

        public int curChapter;

        public int LevelIndex
        {
            get => curIndex;
            set => curIndex = value;
        }

        [SerializeField] private BtnScaleTween scaleTween;

        private void Awake()
        {
            btn.onClick.AddListener(() => { onClick?.Invoke(); });
        }

        private void OnEnable()
        {
            EnsureScaleTween();
            scaleTween?.SyncInteractableState();
            CacheStarImages();
        }

        public void SetSelected(bool selected)
        {
            selectShowObject?.SetActive(selected);
            selectHideObject?.SetActive(!selected);
        }

        public void Show(int chapter, int index)
        {
            curChapter = chapter;
            LevelIndex = index;
            titleText.text = LocalizeKey.GetLevelName(chapter, index);

            LevelPassState passState = GetPassState(chapter, index);
            // 未解锁关卡也允许点击查看详情，是否可挑战由面板上的挑战按钮控制
            btn.interactable = true;
            stateChange.SetState((int)passState);
            UpdateOutlineState(passState);
            UpdatePassTimeText(passState);
            UpdateStarState(passState);
            scaleTween?.SyncInteractableState();
        }

        private LevelPassState GetPassState(int chapter, int index)
        {
            return PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, index);
        }

        /// <summary>
        /// 根据关卡结果刷新三颗星。
        /// </summary>
        private void UpdateStarState(LevelPassState passState)
        {
            if (!CacheStarImages())
            {
                return;
            }

            int starCount = 0;
            if (passState == LevelPassState.Pass)
            {
                starCount = PlayerSaveData.LocalSavaData.levelPassData.GetLevelStarCount(curChapter, LevelIndex);
            }

            for (int i = 0; i < starImages.Length; i++)
            {
                Image starImage = starImages[i];
                if (!starImage)
                {
                    continue;
                }

                bool isOn = i < starCount;
                if (isOn && starOnSprite != null)
                {
                    starImage.sprite = starOnSprite;
                }
                else if (!isOn && starOffSprite != null)
                {
                    starImage.sprite = starOffSprite;
                }

                starImage.enabled = true;
            }
        }

        /// <summary>
        /// 缓存星星图片组件，避免每次刷新都重复查找。
        /// </summary>
        private bool CacheStarImages()
        {
            if (starImages != null && starImages.Length > 0)
            {
                return true;
            }

            if (starsRoot == null)
            {
                starsRoot = transform.Find("stars");
            }

            if (starsRoot == null || starsRoot.childCount == 0)
            {
                return false;
            }

            starImages = new Image[starsRoot.childCount];
            for (int i = 0; i < starsRoot.childCount; i++)
            {
                starImages[i] = starsRoot.GetChild(i).GetComponent<Image>();
            }

            return true;
        }

        /// <summary>
        /// 根据通关状态刷新关卡耗时文本。
        /// </summary>
        private void UpdatePassTimeText(LevelPassState passState)
        {
            // string passTimeStr = "";
            // if (passState != LevelPassState.Pass ||
            //     !PlayerSaveData.LocalSavaData.levelPassData.TryGetPassTime(curChapter, LevelIndex, out float passTime))
            // {
            //     passTimeStr = "--";
            // }
            // else
            // {
            //     passTimeStr = FormatPassTime(passTime);
            // }
            //
        }


        /// <summary>
        /// 将秒数格式化为关卡耗时文本。
        /// </summary>
        private static string FormatPassTime(float passTime)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(passTime));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }

        private void UpdateOutlineState(LevelPassState passState)
        {
            bool showOutlineFlow = passState != LevelPassState.Pass && IsLatestUnlockedLevel(curChapter, LevelIndex);

            if (outlineFlow != null)
            {
                outlineFlow.gameObject.SetActive(showOutlineFlow);
            }

            if (outline != null)
            {
                outline.gameObject.SetActive(!showOutlineFlow);
            }
        }

        private bool IsLatestUnlockedLevel(int chapter, int index)
        {
            var chapterSetting = LubanTables.GetChapterSetting(chapter);

            int latestUnlockedLevel = int.MinValue;
            for (int i = 0; i < chapterSetting.MaxLevel; i++)
            {
                int level = i + 1;
                if (GetPassState(chapter, level) != LevelPassState.Lock)
                {
                    latestUnlockedLevel = Mathf.Max(latestUnlockedLevel, level);
                }
            }

            return latestUnlockedLevel == index;
        }

        private void EnsureScaleTween()
        {
            scaleTween ??= GetComponent<BtnScaleTween>();
            if (scaleTween == null)
            {
                scaleTween = gameObject.AddComponent<BtnScaleTween>();
            }
        }
    }
}
