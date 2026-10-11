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

			DrawScaleStartHint();
		}

		/// <summary>
		/// 缩放事件首帧值提醒:From 与"事件开始时物体的实际大小"对不上时给出文字提示
		/// <para>
		/// 运行时 XCScaleEvent 是在物体实际大小上做差值累加,From 不参与起点赋值,
		/// 所以起点配错时会出现"预览对、运行时不对"
		/// </para>
		/// </summary>
		private void DrawScaleStartHint()
		{
			FTweenScaleEvent scaleEvt = target as FTweenScaleEvent;
			if( scaleEvt == null || !FScaleEventStartChecker.TryGetExpectedStartScale( scaleEvt, out Vector3 expected ) )
				return;

			Vector3 from = scaleEvt.Tween.From;
			if( FScaleEventStartChecker.IsSameScale( from, expected ) )
				return;

			EditorGUILayout.HelpBox(
				$"首帧大小对不上:\n" +
				$"事件开始时物体实际大小应为 {expected.ToString("F3")}(上一段缩放的终点,或物体基础大小),\n" +
				$"当前 From 配的是 {from.ToString("F3")}。\n" +
				$"运行时是在物体实际大小上做差值累加,From 不参与起点赋值,起点对不上会导致编辑器预览与运行时效果不一致。\n" +
				$"把播放头停到本事件首帧后点上面的 Set From 即可回填。",
				MessageType.Warning );
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
