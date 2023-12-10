using UnityEngine;
using UnityEditor;

using Flux;

using FluxEditor;

namespace FluxEditor
{
	[CustomEditor(typeof(FTransformEvent), true)]
	public class FTransformEventInspector : FTweenEventInspector
	{	
		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI();


			if( _tween.isExpanded )
			{
				serializedObject.Update();

				float doubleLineHeight = EditorGUIUtility.singleLineHeight * 2;

				Rect tweenRect = GUILayoutUtility.GetLastRect();

				tweenRect.yMin = tweenRect.yMax - doubleLineHeight;
				tweenRect.height = EditorGUIUtility.singleLineHeight;

				tweenRect.xMin = tweenRect.xMax - 80;

				if( GUI.Button( tweenRect, "Set To", EditorStyles.miniButton ) )
					_to.vector3Value = GetPropertyValue();

				tweenRect.y -= doubleLineHeight+2.5f;

				if( GUI.Button( tweenRect, "Set From", EditorStyles.miniButton ) )
					_from.vector3Value = GetPropertyValue();

				serializedObject.ApplyModifiedProperties();
			}
		}

		public Vector3 GetPropertyValue()
		{
			FTransformEvent transformEvt = (FTransformEvent)target;
			if( transformEvt is FTweenPositionEvent )
				return transformEvt.Owner.localPosition;
			if( transformEvt is FTweenRotationEvent )
				return transformEvt.Owner.localRotation.eulerAngles;
			if( transformEvt is FTweenScaleEvent )
				return transformEvt.Owner.localScale;

			Debug.LogWarning( "Unexpected child of FTransformEvent, setting (0,0,0)" );
			return Vector3.zero;
		}
	}
}
