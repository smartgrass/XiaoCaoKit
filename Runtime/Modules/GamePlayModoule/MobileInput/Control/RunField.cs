using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MFPC
{
    /// <summary>
    /// Allows you to create a button that will set the running state
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Outline))]
    public class RunField : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// Equals True when the button is active
        /// </summary>
        public bool InRunField { get; private set; } = false;

        private RectTransform fieldRectTransform;
        private Outline runFieldOutline;

        #region MONO

        private void Awake()
        {
            fieldRectTransform = this.GetComponent<RectTransform>();
            runFieldOutline = this.GetComponent<Outline>();
        }

        private void OnDisable()
        {
            OnPointerUp(null);
        }

        #endregion

        #region CALLBACK

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnPointerUp(PointerEventData eventData) => runFieldOutline.enabled = InRunField = false;

        public void OnDrag(PointerEventData eventData)
        {
            InRunField = RectTransformUtility.RectangleContainsScreenPoint(fieldRectTransform, eventData.position);
            runFieldOutline.enabled = InRunField;
        }

        #endregion
    }
}