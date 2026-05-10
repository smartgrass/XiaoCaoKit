using UnityEngine;

namespace XiaoCao
{
    public static class UIClickDetector
    {
        public static bool IsPointerOverTarget(Vector2 mousePosition, RectTransform rect)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, mousePosition);
        }
    }
}