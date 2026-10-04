#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;

namespace AssetEditor.Editor
{
    /// <summary>
    /// 数值测试面板:按输入的等级计算并打印角色与圣器的等级数值,仅用于调试。
    /// </summary>
    public class TestValueEditorWindow : XiaoCaoWindow
    {
        private int roleLevel = 1;

        private ERelicType relicType = ERelicType.Weapon;
        private EQuality relicQuality = EQuality.White;
        private int relicLevel = 1;

        private string levelName = "level_0_1";

        [MenuItem(XCEditorTools.TestValueEditorWindow)]
        public static TestValueEditorWindow Open()
        {
            return OpenWindow<TestValueEditorWindow>("数值测试面板");
        }

        /// <summary>
        /// 绘制面板的输入区与输出按钮。
        /// </summary>
        public override void DrawHead()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("数值测试面板", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("输入等级后点击输出,结果会打印到 Console 面板。", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            DrawRoleSection();
            EditorGUILayout.Space();
            DrawRelicSection();
            EditorGUILayout.Space();
            DrawLevelRewardSection();
        }

        /// <summary>
        /// 绘制角色等级测试区。
        /// </summary>
        private void DrawRoleSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("1. 测试角色等级", EditorStyles.boldLabel);
            roleLevel = EditorGUILayout.IntField("角色等级", roleLevel);
            if (GUILayout.Button("输出角色等级数值"))
            {
                OutputRoleLevel();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制圣器数值测试区。
        /// </summary>
        private void DrawRelicSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("2. 测试圣器数值", EditorStyles.boldLabel);
            relicType = (ERelicType)EditorGUILayout.EnumPopup("圣器类型", relicType);
            relicQuality = (EQuality)EditorGUILayout.EnumPopup("圣器品级", relicQuality);
            relicLevel = EditorGUILayout.IntField("圣器等级", relicLevel);
            if (GUILayout.Button("输出圣器数值"))
            {
                OutputRelic();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制关卡奖励测试区。
        /// </summary>
        private void DrawLevelRewardSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("3. 测试关卡奖励", EditorStyles.boldLabel);
            levelName = EditorGUILayout.TextField("关卡名(level_0_1)", levelName);
            if (GUILayout.Button("输出关卡奖励"))
            {
                OutputLevelReward();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 按输入等级计算并打印角色的基础数值与累计经验。
        /// </summary>
        private void OutputRoleLevel()
        {
            int level = Mathf.Max(1, roleLevel);
            Debug.Log($"-- 角色等级数值 level={level} " +
                      $"ATK={CharacterAttrCalc.GetBaseATK(level):0.##} " +
                      $"HP={CharacterAttrCalc.GetBaseHP(level):0.##} " +
                      $"DEF={CharacterAttrCalc.GetBaseDEF(level):0.##} " +
                      $"MaxExp={CharacterAttrCalc.GetNextLevelExp(level):0.##} " +
                      $"TotalExp={CharacterAttrCalc.GetTotalExp(level)}");
        }

        /// <summary>
        /// 按输入的圣器类型、品级与等级计算并打印圣器数值与合成所需材料。
        /// </summary>
        private void OutputRelic()
        {
            int level = Mathf.Max(1, relicLevel);
            string typeId = $"Relic/{relicType}_{(int)relicQuality}_{level}";
            Item item = new Item(ItemType.HolyRelic, typeId, level, relicQuality);

            Dictionary<EAttr, float> valueDic = RelicValueCalc.GetRelicValue(item);
            List<string> valueParts = new List<string>();
            foreach (var kv in valueDic)
            {
                valueParts.Add($"{kv.Key}={kv.Value:0.##}");
            }
            Debug.Log($"-- 圣器数值 {relicType} 品级={relicQuality} 等级={level} {string.Join(", ", valueParts)}");

            List<Item> needItems = ForgeCostCalc.GetNeedItems(item.ToRelicItem());
            Debug.Log($"---- 合成所需材料 {FormatItems(needItems)}");
        }

        /// <summary>
        /// 按输入的关卡名计算并打印通关奖励与首通奖励。
        /// </summary>
        private void OutputLevelReward()
        {
            string key = levelName.Trim();
            Debug.Log($"-- 关卡奖励 {key} 通关奖励 {FormatItems(LevelSettingHelper.GetReward(key))}");
            Debug.Log($"---- 首次奖励 {FormatItems(LevelSettingHelper.GetFirstReward(key))}");
        }

        /// <summary>
        /// 把物品列表拼接成"id x数量"的单行文本。
        /// </summary>
        private static string FormatItems(List<Item> items)
        {
            List<string> parts = new List<string>();
            foreach (Item item in items)
            {
                parts.Add($"{item.typeId} x{item.num}");
            }

            return string.Join(", ", parts);
        }
    }
}
#endif
