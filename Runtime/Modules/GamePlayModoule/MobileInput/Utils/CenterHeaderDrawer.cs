#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace MFPC.Utils
{
    [CustomPropertyDrawer(typeof(CenterHeader))]
    public class SeparatorDrawer : DecoratorDrawer
    {
        public override void OnGUI(Rect position)
        {
            CenterHeader separatorAttribute = attribute as CenterHeader;
            Vector2 sizeOfLabel = new GUIStyle(GUI.skin.box).CalcSize(new GUIContent(separatorAttribute.Header));

            Rect separatorRectLeft = new Rect(position.xMin,
                position.yMin + separatorAttribute.Spacing,
                (position.width / 2f) - (sizeOfLabel.x / 2f),
                separatorAttribute.Height);

            Rect separatorRectRight = new Rect(position.xMin + (position.width / 2f) + (sizeOfLabel.x / 2f),
                position.yMin + separatorAttribute.Spacing,
                (position.width / 2f) - (sizeOfLabel.x / 2f),
                separatorAttribute.Height);

            Rect headerRect = new Rect(position.xMin + (position.width / 2f) - (sizeOfLabel.x / 2f) + 1.5f,
                position.yMin,
                position.width,
                position.height);

            EditorGUI.DrawRect(separatorRectLeft, Color.grey);
            EditorGUI.DrawRect(separatorRectRight, Color.grey);
            EditorGUI.LabelField(headerRect, separatorAttribute.Header);
        }

        public override float GetHeight()
        {
            CenterHeader separatorAttribute = attribute as CenterHeader;

            float totalSpacing = separatorAttribute.Spacing
                + separatorAttribute.Height
                + separatorAttribute.Spacing;

            return totalSpacing;
        }
    }
}
#endif