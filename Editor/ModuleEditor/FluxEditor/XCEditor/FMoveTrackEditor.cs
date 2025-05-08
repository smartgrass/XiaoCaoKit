using UnityEngine;
using UnityEditor;

using Flux;
using System.Collections.Generic;
using XiaoCao;
using XiaoCaoEditor;
using static UnityEngine.GraphicsBuffer;
using static UnityEditor.PlayerSettings;
using NaughtyAttributes.Editor;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace FluxEditor
{
    [FEditor(typeof(FMoveTrack))]
    public class FMoveTrackEditor : FTrackEditor
    {
        public override void UpdateEventsEditor(int frame, float time)
        {
            base.UpdateEventsEditor(frame, time);
        }
    }
    [CustomEditor(typeof(FMoveTrack), true)]
    public class FMoveTrackEditorInspector : FTrackInspector
    {
        private FMoveTrack track;

        public override void OnEnable()
        {
            base.OnEnable();
            track = (FMoveTrack)target;
        }
        private void OnSceneGUI()
        {
            int len = track.Events.Count;
            for (int i = 0; i < len; i++)
            {
                FMoveEventEditor _tmpEditor = Editor.CreateEditor(track.Events[i]) as FMoveEventEditor;
                _tmpEditor.ShowSceneGUI(i);
            }
        }
    }

    ///<see cref="FTransformEventInspector"/>
    [CustomEditor(typeof(FMoveEvent), true)]
    [CanEditMultipleObjects]
    public class FMoveEventEditor : FEventInspector
    {
        private static Color[] colors = new[] { Color.green, Color.yellow, Color.blue };
        private int colorIndex = 0;
        private int index = 0;

        protected override void OnEnable()
        {
            _event = target as FMoveEvent;
            base.OnEnable();
        }

        private FMoveEvent _event;
        private FMoveEvent Event
        {
            get
            {
                if (_event == null)
                {
                    _event = target as FMoveEvent;
                }
                return _event;
            }
        }
        private Quaternion rotation => Event.transform.rotation;

        public void ShowSceneGUI(int index = 0)
        {
            this.index = index;
            colorIndex = index % 3;
            OnSceneGUI();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Reset"))
            {
                ResethandlePonts();
            }
        }


        public void ResethandlePonts()
        {
            Event.handlePionts.Clear();
            Event.CheckLen();
        }



        private bool _isBezierLast;
        private int _lastLen;


        void OnSceneGUI()
        {
            Handles.color = colors[colorIndex];
            if (_isBezierLast != Event.IsBezier)
            {
                Event.OnBezierChange();
            }
            if (_lastLen != Event.controlPoints.Count)
            {
                Event.OnNumChange();
            }

            Handles.color = colors[colorIndex];
            if (Event.IsBezier)
            {
                DrawBezier();
                if (Event.isEditorHandles)
                {
                    XCDraw.DrawLines(Event.controlPoints);
                }
            }
            else
            {
                DrawControls();
                XCDraw.DrawLines(Event.controlPoints);
            }

            _isBezierLast = Event.IsBezier;
            _lastLen = Event.controlPoints.Count;
        }

        private void DrawBezier()
        {
            var ps = Event.controlPoints;

            List<Vector3> points = new List<Vector3>();
            var _handlePoint = Event.handlePionts;

            for (int i = 0; i < _handlePoint.Count; i++)
            {
                XCDraw.DrawBezier(Event.controlPoints[i], Event.controlPoints[i + 1], _handlePoint[i]);
            }

            DrawControls();
            DrawHandlePoints();
        }

        

        //»æÖÆµã
        private void DrawControls()
        {
            for (int i = 0; i < Event.controlPoints.Count; i++)
            {
                Vector3 newPointPosition = Handles.DoPositionHandle(Event.controlPoints[i], rotation);
                if (Event.controlPoints[i] != newPointPosition)
                {
                    Event.controlPoints[i] = newPointPosition;
                }
                Handles.Label(newPointPosition + new Vector3(0, HandleUtility.GetHandleSize(newPointPosition) * 0.4f, 0f), i.ToString());
            }
        }

        private void DrawHandlePoints()
        {
            for (int i = 0; i < Event.handlePionts.Count; i++)
            {
                Vector3 newPointPosition = Event.handlePionts[i];

                if (Event.isEditorHandles)
                {
                    newPointPosition = Handles.DoPositionHandle(Event.handlePionts[i], rotation);
                    if (Event.handlePionts[i] != newPointPosition)
                    {
                        Event.handlePionts[i] = newPointPosition;
                    }
                }

                Handles.CapFunction handle = Handles.SphereHandleCap;
#if UNITY_2022_3_OR_NEWER
                newPointPosition = Handles.FreeMoveHandle(newPointPosition, HandleUtility.GetHandleSize(newPointPosition) * 0.15f, Vector3.one, handle);
#else
                newPointPosition = Handles.FreeMoveHandle(newPointPosition,Quaternion.identity, HandleUtility.GetHandleSize(newPointPosition) * 0.15f, Vector3.one, handle);
#endif

                if (Event.handlePionts[i] != newPointPosition)
                {
                    Event.handlePionts[i] = newPointPosition;
                }
            }
        }
    }



}
