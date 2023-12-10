using UnityEngine;
using UnityEditor;

using Flux;

namespace FluxEditor
{
	[CustomEditor(typeof( FTweenEvent<> ), true)]
	public class FTweenEventInspector : FEventInspector {

		protected SerializedProperty _tween;
		protected SerializedProperty _from;
		protected SerializedProperty _to;

		protected override void OnEnable()
		{
			base.OnEnable();

			_tween = serializedObject.FindProperty("_tween");
			_tween.isExpanded = true;

			_from = _tween.FindPropertyRelative( "_from" );
			_to = _tween.FindPropertyRelative( "_to" );
		}

//		public override void OnInspectorGUI ()
//		{
//			base.OnInspectorGUI ();
//
//			serializedObject.Update();
//
//			EditorGUILayout.PropertyField( _tween );
//			EditorGUI.indentLevel += 1;
//
//			switch( _from.propertyType )
//			{
//			case SerializedPropertyType.Vector2:
//				_from.vector2Value = EditorGUILayout.Vector2Field( ObjectNames.NicifyVariableName(_from.name), _from.vector2Value );
//				_to.vector2Value = EditorGUILayout.Vector2Field( ObjectNames.NicifyVariableName(_to.name), _to.vector2Value );
//				break;
//
//			case SerializedPropertyType.Vector3:
//				_from.vector3Value = EditorGUILayout.Vector3Field( ObjectNames.NicifyVariableName(_from.name), _from.vector3Value );
//				_to.vector3Value = EditorGUILayout.Vector3Field( ObjectNames.NicifyVariableName(_to.name), _to.vector3Value );
//				break;
//
//			case SerializedPropertyType.Vector4:
//				_from.vector4Value = EditorGUILayout.Vector4Field( ObjectNames.NicifyVariableName(_from.name), _from.vector4Value );
//				_to.vector4Value = EditorGUILayout.Vector4Field( ObjectNames.NicifyVariableName(_to.name), _to.vector4Value );
//				break;
//
//			case SerializedPropertyType.Quaternion:
//				Vector3 fromRot = _from.quaternionValue.eulerAngles;
//				Vector3 toRot = _to.quaternionValue.eulerAngles;
//				EditorGUI.BeginChangeCheck();
//				fromRot = EditorGUILayout.Vector3Field( ObjectNames.NicifyVariableName(_from.name), fromRot );
//				toRot = EditorGUILayout.Vector3Field( ObjectNames.NicifyVariableName(_to.name), toRot );
//				if( EditorGUI.EndChangeCheck() )
//				{
//					_from.quaternionValue = Quaternion.Euler( fromRot );
//					_to.quaternionValue = Quaternion.Euler( toRot );
//				}
//				break;
//
//			default:
//				EditorGUILayout.PropertyField(_from);
//				EditorGUILayout.PropertyField(_to);
//				break;
//			}
//
//			EditorGUILayout.PropertyField(_tween.FindPropertyRelative( "_easingType" ));
//			EditorGUI.indentLevel -= 1;
//
//
//			serializedObject.ApplyModifiedProperties();
//		}
	}
}
