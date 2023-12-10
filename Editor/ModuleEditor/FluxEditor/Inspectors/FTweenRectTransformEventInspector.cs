using UnityEngine;
using UnityEditor;

using Flux;

namespace FluxEditor
{
    [CustomEditor(typeof(FTweenRectTransformEvent), true)]
    public class FTweenRectTransformEventInspector : FEventInspector
    {
        private SerializedProperty _tweenPosition;
        private SerializedProperty _tweenAnchorMin;
        private SerializedProperty _tweenAnchorMax;
        private SerializedProperty _tweenPivot;
        private SerializedProperty _tweenRotation;
        private SerializedProperty _tweenScale;

        private FTweenRectTransformEvent _tweenRectTransformEvent;

        protected override void OnEnable()
        {
            base.OnEnable();

            _tweenRectTransformEvent = (FTweenRectTransformEvent) target;

            _tweenPosition = serializedObject.FindProperty("_tweenPosition");
            _tweenAnchorMin = serializedObject.FindProperty("_tweenAnchorMin");
            _tweenAnchorMax = serializedObject.FindProperty("_tweenAnchorMax");
            _tweenPivot = serializedObject.FindProperty("_tweenPivot");
            _tweenRotation = serializedObject.FindProperty("_tweenRotation");
            _tweenScale = serializedObject.FindProperty("_tweenScale");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();


            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set From"))
            {
                Set("_from");
            }

            if (GUILayout.Button("Set To"))
            {
                Set("_to");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void Set(string variable)
        {
            RectTransform rectTransform = _tweenRectTransformEvent.Owner as RectTransform;
            if (rectTransform != null)
            {
                _tweenPosition.FindPropertyRelative(variable).vector3Value = rectTransform.anchoredPosition3D;
                _tweenAnchorMin.FindPropertyRelative(variable).vector2Value = rectTransform.anchorMin;
                _tweenAnchorMax.FindPropertyRelative(variable).vector2Value = rectTransform.anchorMax;
                _tweenPivot.FindPropertyRelative(variable).vector2Value = rectTransform.pivot;
                _tweenRotation.FindPropertyRelative(variable).vector3Value = rectTransform.localRotation.eulerAngles;
                _tweenScale.FindPropertyRelative(variable).vector3Value = rectTransform.localScale;

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
