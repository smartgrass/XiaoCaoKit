using UnityEngine;
using UnityEditor;

using System;
using System.Reflection;

namespace FluxEditor
{
	public class AnimationWindowProxy
    {

        private static readonly Assembly EDITOR_WINDOW_ASSEMBLY = typeof(EditorWindow).Assembly;

		private static readonly Type ANIMATION_WINDOW_TYPE = EDITOR_WINDOW_ASSEMBLY.GetType("UnityEditor.AnimationWindow");
		private static readonly Type ANIMATION_WINDOW_STATE_TYPE = EDITOR_WINDOW_ASSEMBLY.GetType("UnityEditorInternal.AnimationWindowState");

        private static readonly Type ANIMATION_EDITOR_TYPE = EDITOR_WINDOW_ASSEMBLY.GetType("UnityEditor.AnimEditor");
		private static readonly Type ANIMATION_WINDOW_SELECTED_ITEM_TYPE = EDITOR_WINDOW_ASSEMBLY.GetType("UnityEditorInternal.AnimationWindowSelectionItem");

		private static EditorWindow _animationWindow = null;
		public static EditorWindow AnimationWindow {
			get	{
				if( _animationWindow == null )
					_animationWindow = FUtility.GetWindowIfExists( ANIMATION_WINDOW_TYPE );
				return _animationWindow;
			}
		}

		public static EditorWindow OpenAnimationWindow()
		{
			if( _animationWindow == null )
				_animationWindow = EditorWindow.GetWindow( ANIMATION_WINDOW_TYPE );
			return _animationWindow;
		}

		#region AnimationWindow variables

		private static FieldInfo _animEditorField = null;
		private static FieldInfo AnimEditorField {
			get {
				if( _animEditorField == null )
					_animEditorField = ANIMATION_WINDOW_TYPE.GetField( "m_AnimEditor", BindingFlags.Instance | BindingFlags.NonPublic );
				return _animEditorField;
			}
		}

		private static ScriptableObject _animEditor = null;
		private static ScriptableObject AnimEditor {
			get {
				if( _animEditor == null )
					_animEditor = (ScriptableObject)AnimEditorField.GetValue( AnimationWindow );
				return _animEditor;
			}
		}

		private static FieldInfo _stateField = null;
		private static FieldInfo StateField {
			get {
				if( _stateField == null )
					_stateField = ANIMATION_EDITOR_TYPE.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
				return _stateField;
			}

		}

		private static PropertyInfo _animationClipProperty = null;
		private static PropertyInfo AnimationClipProperty {
			get {
				if( _animationClipProperty == null )
					_animationClipProperty = ANIMATION_WINDOW_SELECTED_ITEM_TYPE.GetProperty("animationClip", BindingFlags.Instance | BindingFlags.Public);
				return _animationClipProperty;
			}
		}

        #endregion

        #region AnimationWindowState

        private static MethodInfo _startPreviewAndRecordingMethod = null;
        public static MethodInfo StartPreviewAndRecordingMethod
        {
            get
            {
                if (_startPreviewAndRecordingMethod == null)
                    _startPreviewAndRecordingMethod = ANIMATION_WINDOW_STATE_TYPE.GetMethod("StartRecording", BindingFlags.Instance | BindingFlags.Public);
                return _startPreviewAndRecordingMethod;
            }
        }

        private static MethodInfo _stopPreviewAndRecordingMethod = null;
        public static MethodInfo StopPreviewAndRecordingMethod
        {
            get
            {
                if (_stopPreviewAndRecordingMethod == null)
                    _stopPreviewAndRecordingMethod = ANIMATION_WINDOW_STATE_TYPE.GetMethod("StopPreview", BindingFlags.Instance | BindingFlags.Public);
                return _stopPreviewAndRecordingMethod;
            }
        }

        private static PropertyInfo _isRecordingField = null;
        public static PropertyInfo IsRecordingField
        {
            get
            {
                if (_isRecordingField == null)
                    _isRecordingField = ANIMATION_WINDOW_STATE_TYPE.GetProperty("recording", BindingFlags.Instance | BindingFlags.Public);
                return _isRecordingField;
            }
        }

        

        private static PropertyInfo _selectedItemProperty = null;
        private static PropertyInfo SelectedItemProperty
        {
            get
            {
                if (_selectedItemProperty == null)
                    _selectedItemProperty = ANIMATION_WINDOW_STATE_TYPE.GetProperty("selection", BindingFlags.Instance | BindingFlags.Public);
                return _selectedItemProperty;
            }
        }

        private static PropertyInfo _currentTimeProperty = null;
		private static PropertyInfo CurrentTimeProperty {
			get {
				if( _currentTimeProperty == null )
					_currentTimeProperty = ANIMATION_WINDOW_STATE_TYPE.GetProperty("currentTime", BindingFlags.Instance | BindingFlags.Public);
				return _currentTimeProperty;
			}
		}
		
		private static PropertyInfo _frameProperty = null;
		private static PropertyInfo FrameProperty {
			get {
				if( _frameProperty == null )
					_frameProperty = ANIMATION_WINDOW_STATE_TYPE.GetProperty("currentFrame", BindingFlags.Instance | BindingFlags.Public);
				return _frameProperty;
			}
		}

		#endregion

		public static void StartAnimationMode()
		{
            // now it starts both preview and recording
            StartPreviewAndRecordingMethod.Invoke(GetState(), null);
        }

        public static bool InAnimationMode()
        {
            return (bool)IsRecordingField.GetValue(GetState(), null);
        }

        public static void StopAnimationMode()
        {
            // now it stops preview and recording
            StopPreviewAndRecordingMethod.Invoke(GetState(), null);
        }

		private static object GetState()
		{
            object stateObject = StateField.GetValue( AnimEditor );

            return stateObject;
        }

		private static object GetSelectedItem()
		{
			return SelectedItemProperty.GetValue(GetState(), null);
		}


		public static void SetCurrentFrame( int frame, float time )
		{
			if( AnimationWindow == null )
				return;

			object state = GetState();

			// pass a little nudge because of floating point errors which will make it go
			// to a lower frame instead of the frame we want
			CurrentTimeProperty.SetValue( state, time+0.0001f, null );

			_animationWindow.Repaint();
		}

		public static int GetCurrentFrame()
		{
			if( AnimationWindow == null )
				return -1;
		
			return (int)FrameProperty.GetValue( GetState(), null );
		}

		public static void SelectAnimationClip( AnimationClip clip )
		{
			if( AnimationWindow == null || clip == null )
				return;

			AnimationClip[] clips = AnimationUtility.GetAnimationClips(Selection.activeGameObject);

			int index = 0;
			for( ; index != clips.Length; ++index )
			{
				if( clips[index] == clip )
					break;
			}


			if( index == clips.Length )
			{
				// didn't find
				Debug.LogError("Couldn't find clip " + clip.name);
			}
			else
			{
				// found
                // Since 2019 it needs to stop animation mode in order for the animation window to be able to load new animation
                bool isInAnimationMode = InAnimationMode();
                if (isInAnimationMode)
                    StopAnimationMode();
				AnimationClipProperty.SetValue( GetSelectedItem(), clip, null );
                if(isInAnimationMode)
                    StartAnimationMode();
			}
		}

		public static AnimationClip GetSelectedAnimationClip()
		{
			return AnimationClipProperty.GetValue(GetSelectedItem(), null) as AnimationClip;
		}
	}
}
