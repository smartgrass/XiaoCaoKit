using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    [DisallowMultipleComponent]
    public class BtnScaleTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Hover/Select Scale")]
        public float hoverScale = 1.05f;
        public float tweenDuration = 0.08f;
        public Ease tweenEase = Ease.OutQuad;

        [SerializeField] private Button targetButton;

        private Vector3 _baseScale;
        private bool _baseScaleCached;
        private bool _isHovering;

        private void Awake()
        {
            ResolveReferences();
            CacheBaseScale();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CacheBaseScale();
            _isHovering = false;
            UpdateScale(true);
        }

        private void OnDisable()
        {
            if (transform == null)
            {
                return;
            }

            transform.DOKill();
            if (_baseScaleCached)
            {
                transform.localScale = _baseScale;
            }

            _isHovering = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanAnimate())
            {
                return;
            }

            _isHovering = true;
            UpdateScale();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!CanAnimate())
            {
                return;
            }

            _isHovering = false;
            UpdateScale();
        }

        public void SyncInteractableState()
        {
            ResolveReferences();
            if (targetButton == null || targetButton.interactable)
            {
                return;
            }

            _isHovering = false;
            UpdateScale();
        }

        private bool CanAnimate()
        {
            ResolveReferences();
            return transform != null && targetButton != null && targetButton.interactable;
        }

        private void ResolveReferences()
        {
            if (targetButton != null)
            {
                return;
            }

            var levelBtn = GetComponent<LevelBtn>();
            if (levelBtn != null)
            {
                targetButton = levelBtn.btn;
            }

            targetButton ??= GetComponent<Button>();
        }

        private void CacheBaseScale()
        {
            if (_baseScaleCached || transform == null)
            {
                return;
            }

            _baseScale = transform.localScale;
            _baseScaleCached = true;
        }

        private void UpdateScale(bool immediate = false)
        {
            if (transform == null)
            {
                return;
            }

            CacheBaseScale();
            Vector3 targetScale = _isHovering ? _baseScale * hoverScale : _baseScale;

            transform.DOKill();
            if (immediate)
            {
                transform.localScale = targetScale;
                return;
            }

            transform.DOScale(targetScale, tweenDuration).SetEase(tweenEase);
        }
    }
}
