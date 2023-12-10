using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Flux;
using OdinSerializer.Utilities;
using NaughtyAttributes;
using NaughtyAttributes.Editor;

namespace FluxEditor
{
    public class FSequenceWindowHeader
    {
        // padding on top, bottom, left and right
        public const float PADDING = 5;

        // space between labels and the fields
        public const float LABEL_SPACE = 5;

        // space between elements (i.e. label+field pairs)
        public const float ELEMENT_SPACE = 20;

        // height of the header
        public const float HEIGHT = ONE_LINE_HEIGHT * LINE + PADDING * LINE + PADDING;

        private const float MAX_SEQUENCE_POPUP_WIDTH = 180;
        private const float UPDATE_MODE_FIELD_WIDTH = 100;
        private const float FRAMERATE_FIELD_WIDTH = 40;
        private const float LENGTH_FIELD_WIDTH = 100;

        // window this header belongs to
        private FSequenceEditorWindow _sequenceWindow;

        private SerializedObject _sequenceSO;

        private SerializedProperty _sequenceUpdateMode;
        private SerializedProperty _sequenceLength;

        // sequence selection popup variables
        private GUIContent _sequenceLabel = new GUIContent("Seq ", "Select Sequence...");

        // rect of the sequence label
        private Rect _sequenceLabelRect;

        // rect of the sequence name
        private Rect _sequencePopupRect;

        // rect for the button to create a new sequence
        private Rect _sequenceAddButtonRect;

        private FSequence[] _sequences;

        private GUIContent[] _sequenceNames;

        private int _selectedSequenceIndex;

        // update mode UI variables
        private GUIContent _updateModeLabel = new GUIContent("Update Mode", "How does the sequence update:\n\tNormal: uses Time.time in Update()\n\tAnimatePhysics: uses Time.fixedTime in FixedUpdate()\n\tUnscaledTime: uses Time.unscaledTime in Update()");
        private Rect _updateModeLabelRect;
        private Rect _updateModeFieldRect;
        private bool _showUpdadeMode;

        // framerate UI variables
        private GUIContent _framerateLabel = new GUIContent("Frame Rate", "How many frames does the sequence have per second");
        private Rect _framerateLabelRect;
        private Rect _framerateFieldRect;
        private bool _showFramerate;

        // length UI variables
        private GUIContent _lengthLabel = new GUIContent("Length", "What's the length of the sequence");
        private Rect _lengthLabelRect;
        private Rect _lengthFieldRect;
        private bool _showLength;


        private GUIContent _addContainerLabel = new GUIContent(string.Empty, "Add Container To Sequence");
        private Rect _addContainerRect;
        private bool _showAddContainer;

        private GUIContent _openInspectorLabel = new GUIContent(string.Empty, "Open Flux Inspector");
        private Rect _openInspectorRect;

        private GUIContent _savaDataLabel = new GUIContent(string.Empty, "_savaData");
        private Rect _savaDataRect;
        // cached number field style, since we want numbers centered
        private GUIStyle _numberFieldStyle;

        public FSequenceWindowHeader(FSequenceEditorWindow sequenceWindow)
        {
            _sequenceWindow = sequenceWindow;

            RebuildSequenceList();

            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            _addContainerLabel.image = FUtility.GetFluxTexture("AddFolder.png");
            _openInspectorLabel.image = FUtility.GetFluxTexture("Inspector.png");
            _savaDataLabel.image = FUtility.GetFluxTexture("Save.png");
        }

        private void OnHierarchyChanged()
        {
            RebuildSequenceList();
        }

        private void RebuildSequenceList()
        {
            _sequences = GameObject.FindObjectsOfType<FSequence>();
            System.Array.Sort<FSequence>(_sequences, delegate (FSequence x, FSequence y) { return x.name.CompareTo(y.name); });

            _sequenceNames = new GUIContent[_sequences.Length + 1];
            for (int i = 0; i != _sequences.Length; ++i)
            {
                _sequenceNames[i] = new GUIContent(_sequences[i].name);
            }

            _sequenceNames[_sequenceNames.Length - 1] = new GUIContent("[Create New Sequence]");
            _selectedSequenceIndex = -1;
        }

        public void RebuildLayout(Rect rect)
        {
            rect_0 = rect;
            InitAllRects();
            rect = lineRects[0];
            float width = rect.width;

            _openInspectorRect = rect;
            _savaDataRect = rect;

            _updateModeLabelRect = _updateModeFieldRect = rect;
            _framerateLabelRect = _framerateFieldRect = rect;
            _lengthLabelRect = _lengthFieldRect = rect;

            _updateModeLabelRect.width = EditorStyles.label.CalcSize(_updateModeLabel).x + LABEL_SPACE;
            _updateModeFieldRect.width = UPDATE_MODE_FIELD_WIDTH;

            _framerateLabelRect.width = EditorStyles.label.CalcSize(_framerateLabel).x + LABEL_SPACE;
            _framerateFieldRect.width = FRAMERATE_FIELD_WIDTH;

            _lengthLabelRect.width = EditorStyles.label.CalcSize(_lengthLabel).x + LABEL_SPACE;
            _lengthFieldRect.width = LENGTH_FIELD_WIDTH * 0.6f;


            _sequenceLabelRect = rect;
            _sequenceLabelRect.width = EditorStyles.label.CalcSize(_sequenceLabel).x + LABEL_SPACE;

            _sequencePopupRect = rect;
            _sequencePopupRect.xMin = _sequenceLabelRect.xMax;
            _sequencePopupRect.width = Mathf.Min(width - _sequenceLabelRect.width, MAX_SEQUENCE_POPUP_WIDTH);
            //			Debug.Log( _sequenceNameRect.width );

            _sequenceAddButtonRect = rect;
            _sequenceAddButtonRect.xMin = _sequencePopupRect.xMax + LABEL_SPACE;
            _sequenceAddButtonRect.width = 16;

            float reminderWidth = width - _sequenceAddButtonRect.xMax;

            _addContainerRect = new Rect(0, 3, 22, 22);
            _addContainerRect.x = _sequencePopupRect.xMax + LABEL_SPACE;

            reminderWidth -= (ELEMENT_SPACE + _addContainerRect.width);

            _showAddContainer = reminderWidth >= 0;

            _openInspectorRect.xMin = _openInspectorRect.xMax - 22;
            //保存按钮
            _savaDataRect.xMax = _openInspectorRect.xMin - 4;
            _savaDataRect.xMin = _savaDataRect.xMax - 24;

            reminderWidth -= 22 + PADDING;

            _lengthFieldRect.x = rect.xMax - 50 - PADDING - _lengthFieldRect.width;
            _lengthLabelRect.x = _lengthFieldRect.xMin - _lengthLabelRect.width;

            reminderWidth -= (ELEMENT_SPACE + _lengthLabelRect.width + _lengthFieldRect.width);

            _showLength = reminderWidth >= 0;

            _framerateFieldRect.x = _lengthLabelRect.xMin - ELEMENT_SPACE - _framerateFieldRect.width;
            _framerateLabelRect.x = _framerateFieldRect.xMin - _framerateLabelRect.width;

            reminderWidth -= (_framerateLabelRect.width + _framerateFieldRect.width + ELEMENT_SPACE);

            _showFramerate = reminderWidth >= 0;

            _updateModeFieldRect.x = _framerateLabelRect.xMin - ELEMENT_SPACE - _updateModeFieldRect.width;
            _updateModeLabelRect.x = _updateModeFieldRect.xMin - _updateModeLabelRect.width;

            reminderWidth -= (_updateModeLabelRect.width + _updateModeFieldRect.width + ELEMENT_SPACE);

            _showUpdadeMode = reminderWidth >= 0;

            _numberFieldStyle = new GUIStyle(EditorStyles.numberField);
            _numberFieldStyle.alignment = TextAnchor.MiddleCenter;
        }
        #region XiaoCao
        public const int LINE = 2;
        //左上角为(0,0)
        public Rect rect_0;
        public Rect[] lineRects = new Rect[LINE];
        public Rect[] xcRects = new Rect[10];
        private List<SeqHeaderShowAttribute> _showAttrs;
        private List<SerializedProperty> _showPros;
        public const int ONE_LINE_HEIGHT = 20;


        private void DrawAllShow()
        {
            int len = _showAttrs.Count;
            for (int i = 0; i < len; i++)
            {
                var pro = _showPros[i];
                var attr = _showAttrs[i];
                var rect = GetWithRect(attr.line, attr.posX, attr.width);
                GUIContent content = attr.GetShowContent(pro);


                var labelRect = rect;
                labelRect.width = EditorStyles.label.CalcSize(content).x;
                if (labelRect.width > attr.width * 0.4f)
                {
                    labelRect.width = attr.width * 0.4f;
                }
                labelRect.y += 2;

                rect.xMin = labelRect.xMax + LABEL_SPACE;
                EditorGUI.PrefixLabel(labelRect, content);
                EditorGUI.PropertyField(rect, pro, GUIContent.none);

            }
        }

        private void InitAllRects()
        {
            for (int i = 0; i < LINE; i++)
            {
                GetLineRect(i);
            }
        }

        private void GetLineRect(int i)
        {
            Rect rect = rect_0;
            float ymin = PADDING + (ONE_LINE_HEIGHT + PADDING) * i;
            rect.yMin = ymin;
            rect.yMax = ymin + ONE_LINE_HEIGHT;
            rect.xMax -= PADDING;
            rect.xMin += PADDING;
            lineRects[i] = rect;
        }

        private Rect GetWithRect(int line, int posX, int width)
        {
            Rect rect = lineRects[line];
            rect.xMin = posX;
            rect.xMax = posX + width;
            return rect;
        }

        #endregion
        public void OnGUI()
        {
            FSequence sequence = _sequenceWindow.GetSequenceEditor().Sequence;

            if ((_selectedSequenceIndex < 0 && sequence != null) || (_selectedSequenceIndex >= 0 && _sequences[_selectedSequenceIndex] != sequence))
            {
                for (int i = 0; i != _sequences.Length; ++i)
                {
                    if (_sequences[i] == sequence)
                    {
                        _selectedSequenceIndex = i;
                        break;
                    }
                }
            }


            if (Event.current.type == EventType.MouseDown && Event.current.alt && _sequencePopupRect.Contains(Event.current.mousePosition))
            {
                Selection.activeObject = sequence;
                Event.current.Use();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.PrefixLabel(_sequenceLabelRect, _sequenceLabel);
            int newSequenceIndex = EditorGUI.Popup(_sequencePopupRect, _selectedSequenceIndex, _sequenceNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (newSequenceIndex == _sequenceNames.Length - 1)
                {
                    FSequence newSequence = FSequenceEditorWindow.CreateSequence();
                    Selection.activeTransform = newSequence.transform;
                    _sequenceWindow.GetSequenceEditor().OpenSequence(newSequence);
                }
                else
                {
                    _selectedSequenceIndex = newSequenceIndex;
                    _sequenceWindow.GetSequenceEditor().OpenSequence(_sequences[_selectedSequenceIndex]);
                    _sequenceWindow.RemoveNotification();
                }
                EditorGUIUtility.keyboardControl = 0; // deselect it
                EditorGUIUtility.ExitGUI();
            }

            // if we're in play mode, can't change anything
            if (Application.isPlaying)
                GUI.enabled = false;

            if (sequence == null)
                return;

            if (_sequenceSO == null || _sequenceSO.targetObject != sequence)
            {
                _sequenceSO = new SerializedObject(sequence);
                _sequenceUpdateMode = _sequenceSO.FindProperty("_updateMode");
                _sequenceLength = _sequenceSO.FindProperty("_length");

                _showPros = new();
                _showAttrs = new();
                PropertyUtility.GetAttrProAndAttrs<SeqHeaderShowAttribute>(_sequenceSO, ref _showPros, ref _showAttrs);
            }

            _sequenceSO.Update();

            if (_showUpdadeMode)
            {
                EditorGUI.PrefixLabel(_updateModeLabelRect, _updateModeLabel);
                EditorGUI.PropertyField(_updateModeFieldRect, _sequenceUpdateMode, GUIContent.none);
            }

            if (_showFramerate)
            {
                EditorGUI.PrefixLabel(_framerateLabelRect, _framerateLabel);
                EditorGUI.BeginChangeCheck();
                int newFrameRate = FGUI.FrameRatePopup(_framerateFieldRect, sequence.FrameRate);
                if (EditorGUI.EndChangeCheck())
                {
                    if (newFrameRate == -1)
                    {
                        FChangeFrameRateWindow.Show(new Vector2(_framerateLabelRect.xMin, _framerateLabelRect.yMax), sequence, FSequenceInspector.Rescale);
                    }
                    else
                    {
                        FSequenceInspector.Rescale(sequence, newFrameRate, true);
                    }
                }
            }

            if (_showLength)
            {
                EditorGUI.PrefixLabel(_lengthLabelRect, _lengthLabel);
                _sequenceLength.intValue = Mathf.Clamp(EditorGUI.IntField(_lengthFieldRect, _sequenceLength.intValue, _numberFieldStyle), 1, int.MaxValue);
            }

            GUIStyle s = new GUIStyle(EditorStyles.miniButton);
            s.padding = new RectOffset(1, 1, 1, 1);

            if (_showAddContainer)
            {
                if (FGUI.Button(_addContainerRect, _addContainerLabel))
                {
                    AddContainer();
                }
            }

            if (FGUI.Button(_openInspectorRect, _openInspectorLabel))
            {
                FInspectorWindow.Open();
            }

            if (FGUI.Button(_savaDataRect, _savaDataLabel))
            {
                Debug.Log($"yns Sava");
                //SavaSequenceData.GetData();
            }

            DrawAllShow();

            _sequenceSO.ApplyModifiedProperties();

            GUI.enabled = true;
        }

        private void AddContainer()
        {
            GenericMenu menu = new GenericMenu();

            bool hasDefaultContainers = false;

            List<FColorSetting> defaultContainers = FUtility.GetSettings().DefaultContainers;
            foreach (FColorSetting colorSetting in defaultContainers)
            {
                if (string.IsNullOrEmpty(colorSetting._str))
                    continue;

                menu.AddItem(new GUIContent(colorSetting._str), false, CreateContainer, colorSetting);
                hasDefaultContainers = true;
            }

            if (!hasDefaultContainers)
            {
                _sequenceWindow.GetSequenceEditor().CreateContainer(new FColorSetting("Default", FGUI.DefaultContainerColor()));
                return;
            }

            menu.AddSeparator(null);

            menu.AddItem(new GUIContent("[Default New Container]"), false, CreateContainer, new FColorSetting("Default", FGUI.DefaultContainerColor()));

            menu.ShowAsContext();
        }

        private void CreateContainer(object data)
        {
            _sequenceWindow.GetSequenceEditor().CreateContainer((FColorSetting)data);
        }
    }
}
