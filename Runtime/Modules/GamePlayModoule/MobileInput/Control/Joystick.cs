using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MFPC
{
    /// <summary>
    /// Sets the direction of movement depending on the direction of the joystick stick
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class Joystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        // Notifies about changes in motion vector
        public event Action<Vector2> OnJoystickDragged;

        //Joystick components
        [SerializeField] protected Image joystick, handle;

        //Time after how many seconds the joystick will change the transparency to 0
        [SerializeField] protected float timeToFadeIn = 0.5f;

        //If it is True, then the joystick will always be in the same place
        [SerializeField] private bool fixedJoystick;

        //If it is True, then the joystick will change transparency
        [SerializeField] private bool fadingJoystick = true;

        private Vector2 InputV;
        protected Color mainColor;
        private Vector2 StartJostick;
        private float currentColorAlpha;
        private RectTransform background;
        private bool doFade;

        public Vector2 GetInputV
        {
            get
            {
                if (doFade)
                {
                    return Vector2.zero;
                }
                return InputV;
            }
        }

        #region MONO

        protected virtual void Awake()
        {
            mainColor = Color.white;
            handle.color = Color.white;
            background = joystick.rectTransform;

            if (fadingJoystick) ChangeJoystickAlpha(0f);
            OnRectTransformDimensionsChange();
        }

        #endregion

        private void Update()
        {
            FadingJoystick();
        }

        /// <summary>
        /// Smoothly changes the transparency of the entire joystick to 0
        /// </summary>
        protected virtual void FadingJoystick()
        {
            if (!doFade) return;

            if (joystick.color.a <= 0.01f || !fadingJoystick)
            {
                background.localPosition = StartJostick; return;
            }

            ChangeJoystickAlpha(Mathf.SmoothDamp(mainColor.a, 0f, ref currentColorAlpha, timeToFadeIn));
        }

        /// <summary>
        /// Changes the transparency of the entire joystick
        /// </summary>
        /// <param name="alpha">Transparent joystick</param>
        protected virtual void ChangeJoystickAlpha(float alpha)
        {
            mainColor.a = alpha;
            joystick.color = mainColor;
            handle.color = mainColor;
        }

        #region CALLBACK

        public virtual void OnPointerUp(PointerEventData point)
        {
            doFade = true;
            handle.rectTransform.anchoredPosition = Vector2.zero;
            OnJoystickDragged?.Invoke(Vector2.zero);
        }

        public virtual void OnPointerDown(PointerEventData point)
        {
            doFade = false;

            if (!fixedJoystick) joystick.rectTransform.position = point.position;

            OnDrag(point);
            ChangeJoystickAlpha(1f);
        }

        public virtual void OnDrag(PointerEventData point)
        {
            //Joystick stick movements
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystick.rectTransform, point.position, point.pressEventCamera, out Vector2 pos))
            {
                pos.x = (pos.x / joystick.rectTransform.sizeDelta.x);
                pos.y = (pos.y / joystick.rectTransform.sizeDelta.y);

                InputV = new Vector2(pos.x * 2, pos.y * 2);
                InputV = (InputV.magnitude > 1.0f) ? InputV.normalized : InputV;

                handle.rectTransform.anchoredPosition = new Vector2(InputV.x * (joystick.rectTransform.sizeDelta.x / 2), InputV.y * (joystick.rectTransform.sizeDelta.y / 2));

                OnJoystickDragged?.Invoke(InputV);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (background != null) StartJostick = background.localPosition;
        }

        #endregion
    }
}
