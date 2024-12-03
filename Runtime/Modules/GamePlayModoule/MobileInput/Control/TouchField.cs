using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace MFPC
{
    public class TouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// The distance between the position of the wheelbarrow on the previous frame and the current one
        /// </summary>
        public Vector2 GetSwipeDirection
        {
            get => new Vector2(touchDist.x, -touchDist.y);
        }

        private Vector2 touchDist;
        private Vector2 pointerOld;
        private bool isPressed;

#if !ENABLE_INPUT_SYSTEM
        private int pointerId;
#else
        private TouchControl touchControl;
#endif

        private void FixedUpdate()
        {
            if (isPressed)
            {
#if !ENABLE_INPUT_SYSTEM
                //Find the distance between the position of the wheelbarrow on the previous frame and the current one
                if (pointerId >= 0 && pointerId < UnityEngine.Input.touches.Length)
                {
                    touchDist = UnityEngine.Input.touches[pointerId].position - pointerOld;
                    pointerOld = UnityEngine.Input.touches[pointerId].position;
                }
                else
                {
                    touchDist =
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               new Vector2(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y) - pointerOld;
                    pointerOld = UnityEngine.Input.mousePosition;
                }
#else
                if (Application.isMobilePlatform && Touchscreen.current != null)
                {
                    Vector2 touchPosition = touchControl.position.ReadValue();
                    touchDist = touchPosition - pointerOld;
                    pointerOld = touchPosition;
                }
                else
                {
                    touchDist = Mouse.current.position.ReadValue() - pointerOld;
                    pointerOld = Mouse.current.position.ReadValue();
                }
#endif
            }
            else touchDist = new Vector2();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            pointerOld = eventData.position;

#if !ENABLE_INPUT_SYSTEM
            pointerId = eventData.pointerId;
#else
            if (!Application.isMobilePlatform || Touchscreen.current == null) return;
            
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.position.ReadValue() == eventData.position)
                {
                    touchControl = touch;
                    break;
                }
            }
            
            pointerOld = touchControl.position.ReadValue();
#endif
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;

            touchDist = Vector2.zero;
        }
    }
}