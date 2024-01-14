using UnityEngine;

namespace Flux
{
    [FEvent(UnUsing+"Transform/Tween Rect Transform", typeof(FTransformTrack))]
    public class FTweenRectTransformEvent : FEvent
    {
        [SerializeField] private FTweenVector3 _tweenPosition;
        [SerializeField] private FTweenVector2 _tweenAnchorMin;
        [SerializeField] private FTweenVector2 _tweenAnchorMax;
        [SerializeField] private FTweenVector2 _tweenPivot;
        [SerializeField] private FTweenVector3 _tweenRotation;
        [SerializeField] private FTweenVector3 _tweenScale;

        private RectTransform _ownerAsRectTransform;

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
            _tweenPosition = new FTweenVector3(Vector3.zero, Vector3.zero);
            _tweenAnchorMin = new FTweenVector2(Vector2.zero, Vector2.zero);
            _tweenAnchorMax = new FTweenVector2(Vector2.zero, Vector2.zero);
            _tweenPivot = new FTweenVector2(Vector2.zero, Vector2.zero);
            _tweenRotation = new FTweenVector3(Vector3.zero, Vector3.zero);
            _tweenScale = new FTweenVector3(Vector3.one, Vector3.one);
        }

        protected override void OnTrigger(float timeSinceTrigger)
        {
            _ownerAsRectTransform = Owner as RectTransform;

            OnUpdateEvent(timeSinceTrigger);
        }

        protected override void OnUpdateEvent(float timeSinceTrigger)
        {
            float t = timeSinceTrigger / LengthTime;

            ApplyProperties(t);
        }

        protected override void OnFinish()
        {
            ApplyProperties(1f);
        }

        protected override void OnStop()
        {
            ApplyProperties(0f);
        }

        private void ApplyProperties(float t)
        {
            if(_ownerAsRectTransform == null) { return; }
            
            _ownerAsRectTransform.anchoredPosition3D = _tweenPosition.GetValue(t);
            _ownerAsRectTransform.anchorMin = _tweenAnchorMin.GetValue(t);
            _ownerAsRectTransform.anchorMax = _tweenAnchorMax.GetValue(t);
            _ownerAsRectTransform.pivot = _tweenPivot.GetValue(t);
            _ownerAsRectTransform.localRotation = Quaternion.Euler(_tweenRotation.GetValue(t));
            _ownerAsRectTransform.localScale = _tweenScale.GetValue(t);

            _ownerAsRectTransform.ForceUpdateRectTransforms();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _ownerAsRectTransform.transform.position = _ownerAsRectTransform.transform.position+new Vector3(0.1f, 0f, 0f); // hack because something is wrong with ugui updating
            }
#endif
        }
    }
}
