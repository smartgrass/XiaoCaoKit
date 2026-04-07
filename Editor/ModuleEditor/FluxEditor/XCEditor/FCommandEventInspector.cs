using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Flux;
using XiaoCao;

namespace FluxEditor
{
    [CustomEditor(typeof(FCommandEvent), true)]
    [CanEditMultipleObjects]
    public class FCommandEventInspector : FEventInspector
    {
        private const string MoveToTargetPosCommandName = "XCCommand_MoveToTargetPos";
        private const string ShootToken = "Shoot";
        private const string DropDownToken = "dropDown";
        private static readonly string[] AtkWarmingPresetValues =
        {
            "AtkWarming",
            "2",
            "Assets/_Res/SkillPrefab/Player/atk_warming_circle.prefab"
        };

        private static readonly string[] OtherMsgPresetDisplayNames =
        {
            "Choose Template...",
            "AtkWarming"
        };

        private static readonly string[][] OtherMsgPresetValues =
        {
            Array.Empty<string>(),
            AtkWarmingPresetValues
        };

        private SerializedProperty commandNameProperty;
        private SerializedProperty baseMsgProperty;
        private SerializedProperty otherMsgsProperty;

        private string[] commandValues = new string[0];
        private string[] commandDisplayNames = new string[0];
        private int selectedCommandIndex;
        private int selectedPresetIndex;

        protected override void OnEnable()
        {
            base.OnEnable();

            commandNameProperty = serializedObject.FindProperty("commandName");
            baseMsgProperty = serializedObject.FindProperty("baseMsg");
            otherMsgsProperty = serializedObject.FindProperty("otherMsgs");

            RefreshCommandOptions();
        }

        protected override void DrawEventFields()
        {
            SyncSelectedCommandIndex();

            DrawCommandPopup();
            DrawBaseMsg();
            DrawOtherMsgs();
        }

        private void DrawCommandPopup()
        {
            if (commandNameProperty == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            selectedCommandIndex = EditorGUILayout.Popup("Command Name", selectedCommandIndex, commandDisplayNames);
            if (EditorGUI.EndChangeCheck() && selectedCommandIndex >= 0 && selectedCommandIndex < commandValues.Length)
            {
                commandNameProperty.stringValue = commandValues[selectedCommandIndex];
            }
        }

        private void DrawBaseMsg()
        {
            if (baseMsgProperty != null)
            {
                EditorGUILayout.PropertyField(baseMsgProperty, true);
                DrawMoveToTargetPosBaseMsgQuickConfig();
            }
        }

        private void DrawMoveToTargetPosBaseMsgQuickConfig()
        {
            if (!string.Equals(commandNameProperty?.stringValue, MoveToTargetPosCommandName, StringComparison.Ordinal))
            {
                return;
            }

            SerializedProperty strMsgProperty = baseMsgProperty.FindPropertyRelative("strMsg");
            if (strMsgProperty == null)
            {
                return;
            }

            List<string> tokens = ParseBaseMsgTokens(strMsgProperty.stringValue);
            bool hasShoot = ContainsToken(tokens, ShootToken);
            bool hasDropDown = ContainsToken(tokens, DropDownToken);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("BaseMsg Quick Config", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("勾选后会同步写回 baseMsg.strMsg，保留 offset_/min_ 等其他参数。", MessageType.None);

            EditorGUI.BeginChangeCheck();
            bool nextShoot = EditorGUILayout.ToggleLeft("Shoot", hasShoot);
            bool nextDropDown = EditorGUILayout.ToggleLeft("DropDown", hasDropDown);
            if (EditorGUI.EndChangeCheck())
            {
                strMsgProperty.stringValue = BuildMoveToTargetPosStrMsg(tokens, nextShoot, nextDropDown);
            }
        }

        private void DrawOtherMsgs()
        {
            if (otherMsgsProperty == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(otherMsgsProperty, true);

            if (!string.Equals(commandNameProperty?.stringValue, MoveToTargetPosCommandName, StringComparison.Ordinal))
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("OtherMsgs Quick Config", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"格式示例: [{string.Join(", ", AtkWarmingPresetValues.Select(x => $"\"{x}\""))}]", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            selectedPresetIndex = EditorGUILayout.Popup("Template", selectedPresetIndex, OtherMsgPresetDisplayNames);
            using (new EditorGUI.DisabledScope(selectedPresetIndex == 0))
            {
                if (GUILayout.Button("Add", GUILayout.Width(80)))
                {
                    AppendOtherMsgs(OtherMsgPresetValues[selectedPresetIndex]);
                }

                if (GUILayout.Button("Replace", GUILayout.Width(80)))
                {
                    ReplaceOtherMsgs(OtherMsgPresetValues[selectedPresetIndex]);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void AppendOtherMsgs(string[] values)
        {
            if (values == null || values.Length == 0 || otherMsgsProperty == null)
            {
                return;
            }

            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                int newIndex = otherMsgsProperty.arraySize;
                otherMsgsProperty.InsertArrayElementAtIndex(newIndex);
                otherMsgsProperty.GetArrayElementAtIndex(newIndex).stringValue = value;
            }
        }

        private void ReplaceOtherMsgs(string[] values)
        {
            if (values == null || values.Length == 0 || otherMsgsProperty == null)
            {
                return;
            }

            otherMsgsProperty.arraySize = 0;
            AppendOtherMsgs(values);
        }

        private void RefreshCommandOptions()
        {
            List<string> names = new List<string> { string.Empty };

            if (XCCommandBinder.Inst != null)
            {
                names.AddRange(XCCommandBinder.Inst.GetAllCommandTypes().Keys.OrderBy(x => x));
            }

            commandValues = names.ToArray();
            commandDisplayNames = commandValues
                .Select(x => string.IsNullOrEmpty(x) ? "Choose Command..." : x)
                .ToArray();

            SyncSelectedCommandIndex();
        }

        private void SyncSelectedCommandIndex()
        {
            string currentName = commandNameProperty == null ? string.Empty : commandNameProperty.stringValue;
            selectedCommandIndex = Array.IndexOf(commandValues, currentName);
            if (selectedCommandIndex < 0)
            {
                selectedCommandIndex = 0;
            }
        }

        private static List<string> ParseBaseMsgTokens(string strMsg)
        {
            if (string.IsNullOrWhiteSpace(strMsg))
            {
                return new List<string>();
            }

            return strMsg
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }

        private static bool ContainsToken(IEnumerable<string> tokens, string targetToken)
        {
            return tokens.Any(x => string.Equals(x, targetToken, StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildMoveToTargetPosStrMsg(IEnumerable<string> tokens, bool includeShoot, bool includeDropDown)
        {
            List<string> result = tokens
                .Where(x => !string.Equals(x, ShootToken, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(x, DropDownToken, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (includeShoot)
            {
                result.Insert(0, ShootToken);
            }

            if (includeDropDown)
            {
                result.Insert(includeShoot ? 1 : 0, DropDownToken);
            }

            return string.Join(",", result);
        }
    }
}
