using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public class ExtraSkillWheelOption : MonoBehaviour
    {
        public Image image;
        public Image selectImage;
        public TMP_Text label;

        private RectTransform _rectTransform;
        private Sprite _defaultSprite;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = transform as RectTransform;
                }

                return _rectTransform;
            }
        }

        public int ItemIndex { get; private set; }

        public bool IsCancel { get; private set; }

        public Vector2 Direction => RectTransform != null ? RectTransform.anchoredPosition.normalized : Vector2.zero;

        private void Awake()
        {
            CacheReferences();
            SetSelected(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheReferences();
            SetSelected(false);
        }
#endif

        public void SetupItem(int itemIndex, Sprite iconSprite, Vector2 size, Vector2 anchoredPos)
        {
            ApplyBaseState(itemIndex, false, $"ExtraSkillOption_{itemIndex}", size, anchoredPos);

            if (image != null)
            {
                image.sprite = iconSprite;
            }

            SetLabelVisible(false);
        }

        public void SetupCancel(int itemIndex, Vector2 size, Vector2 anchoredPos)
        {
            ApplyBaseState(itemIndex, true, "ExtraSkillOption_Cancel", size, anchoredPos);

            if (image != null)
            {
                image.sprite = _defaultSprite;
            }

            SetLabelVisible(true);
        }

        public void RefreshVisual(bool isSelected)
        {
            CacheReferences();
            SetSelected(isSelected);

            if (RectTransform != null)
            {
                RectTransform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
            }
        }

        private void CacheReferences()
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>(true);
            }

            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            if (_defaultSprite == null && image != null)
            {
                _defaultSprite = image.sprite;
            }
        }

        private void ApplyBaseState(int itemIndex, bool isCancel, string optionName, Vector2 size, Vector2 anchoredPos)
        {
            CacheReferences();

            ItemIndex = itemIndex;
            IsCancel = isCancel;
            gameObject.name = optionName;

            if (RectTransform != null)
            {
                RectTransform.sizeDelta = size;
                RectTransform.anchoredPosition = anchoredPos;
                RectTransform.localScale = Vector3.one;
            }

            if (image != null)
            {
                image.raycastTarget = false;
            }

            if (selectImage != null)
            {
                selectImage.raycastTarget = false;
            }

            if (label != null)
            {
                label.raycastTarget = false;
            }

            SetSelected(false);
        }

        private void SetLabelVisible(bool isVisible)
        {
            if (label != null)
            {
                label.gameObject.SetActive(isVisible);
            }
        }

        private void SetSelected(bool isSelected)
        {
            if (selectImage != null)
            {
                selectImage.gameObject.SetActive(isSelected);
            }
        }
    }
}
