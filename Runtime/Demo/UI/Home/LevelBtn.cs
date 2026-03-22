using System;
using cfg;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XiaoCao;
using XiaoCaoKit;
using XiaoCaoKit.UI;

namespace XiaoCao.UI
{
    public class LevelBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TMP_Text titleText;
        public Button btn;
        public UIStateChange stateChange;
        public Action onClick;
        public Transform rewardParent;
        public Transform outlineFlow;
        public Transform outline;

        [Header("Hover/Select Scale")] public float hoverScale = 1.05f;
        public float tweenDuration = 0.08f;
        public Ease tweenEase = Ease.OutQuad;

        public int curChapter;
        public int levelIndex;

        private Vector3 _baseScale;
        private bool _baseScaleCached;
        private bool _isHovering;

        private void Awake()
        {
            btn.onClick.AddListener(() => { onClick?.Invoke(); });
            CacheBaseScale();
        }

        private void OnEnable()
        {
            CacheBaseScale();
            UpdateScale();
        }

        public void Show(int chapter, int index)
        {
            curChapter = chapter;
            levelIndex = index;
            titleText.text = LocalizeKey.GetLevelName(chapter, index);

            LevelPassState passState = GetPassState(chapter, index);
            btn.interactable = passState != LevelPassState.Lock;
            stateChange.SetState((int)passState);
            UpdateOutlineState(passState);
            if (!btn.interactable)
            {
                _isHovering = false;
            }

            UpdateScale();
            UpdateReward();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (btn == null || !btn.interactable)
            {
                return;
            }

            _isHovering = true;
            UpdateScale();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (btn == null || !btn.interactable)
            {
                return;
            }

            _isHovering = false;
            UpdateScale();
        }

        private void CacheBaseScale()
        {
            if (_baseScaleCached)
            {
                return;
            }

            _baseScale = transform.localScale;
            _baseScaleCached = true;
        }

        private void UpdateScale()
        {
            CacheBaseScale();
            Vector3 targetScale = _isHovering ? _baseScale * hoverScale : _baseScale;
            transform.DOKill();
            transform.DOScale(targetScale, tweenDuration).SetEase(tweenEase);
        }

        private LevelPassState GetPassState(int chapter, int index)
        {
            return PlayerSaveData.LocalSavaData.levelPassData.GetPassState(chapter, index);
        }

        private void UpdateOutlineState(LevelPassState passState)
        {
            bool showOutlineFlow = passState != LevelPassState.Pass && IsLatestUnlockedLevel(curChapter, levelIndex);

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
            if (chapterSetting?.Levels == null || chapterSetting.Levels.Count == 0)
            {
                return false;
            }

            int latestUnlockedLevel = int.MinValue;
            for (int i = 0; i < chapterSetting.Levels.Count; i++)
            {
                int level = chapterSetting.Levels[i];
                if (GetPassState(chapter, level) != LevelPassState.Lock)
                {
                    latestUnlockedLevel = Mathf.Max(latestUnlockedLevel, level);
                }
            }

            return latestUnlockedLevel == index;
        }

        private void UpdateReward()
        {
            if (rewardParent == null)
            {
                return;
            }

            string levelKey = MapNames.GetLevelKey(curChapter, levelIndex);
            var rewards = LevelSettingHelper.GetReward(levelKey);
            if (rewards == null)
            {
                UITool.SetCellListCount(rewardParent, 0);
                return;
            }

            UITool.SetCellListCount(rewardParent, rewards.Count);
            var cells = rewardParent.GetComponentsInChildren<ItemCell>(false);
            for (int i = 0; i < rewards.Count && i < cells.Length; i++)
            {
                cells[i].SetItem(rewards[i]);
            }
        }
    }
}