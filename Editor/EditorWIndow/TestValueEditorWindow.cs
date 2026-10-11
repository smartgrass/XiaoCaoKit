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

        //圣器合成材料计算: 等级 / 品级 / 类型(默认全选)
        private int materialLevel = 1;
        private EQuality materialQuality = EQuality.White;
        private readonly Dictionary<ERelicType, bool> materialTypeSelected = new Dictionary<ERelicType, bool>();
        //圣器类型全集, 供"类型"多选使用
        private static readonly ERelicType[] AllRelicTypes = (ERelicType[])System.Enum.GetValues(typeof(ERelicType));

        private string levelName = "level_0_1";

        private int curveTargetLevel = 30;
        private int curveStartLevel = 1;
        private int curveStartExp = 0;
        private string lastCurveCsvPath = string.Empty;

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
            DrawRelicMaterialSection();
            EditorGUILayout.Space();
            DrawLevelRewardSection();
            EditorGUILayout.Space();
            DrawLevelCurveSection();
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
        /// 绘制圣器合成材料计算区, 输入等级、品级与类型(默认全选)。
        /// </summary>
        private void DrawRelicMaterialSection()
        {
            EnsureMaterialTypesSelected();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("3. 圣器合成材料计算", EditorStyles.boldLabel);
            materialLevel = EditorGUILayout.IntField("等级", materialLevel);
            materialQuality = (EQuality)EditorGUILayout.EnumPopup("品级", materialQuality);

            EditorGUILayout.LabelField("类型(默认全选)", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (ERelicType type in AllRelicTypes)
            {
                materialTypeSelected[type] =
                    EditorGUILayout.ToggleLeft(type.ToString(), materialTypeSelected[type], GUILayout.Width(76));
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                SetAllMaterialTypes(true);
            }

            if (GUILayout.Button("全不选", GUILayout.Width(60)))
            {
                SetAllMaterialTypes(false);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("输出合成材料"))
            {
                OutputRelicMaterials();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制关卡奖励测试区。
        /// </summary>
        private void DrawLevelRewardSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("4. 测试关卡奖励", EditorStyles.boldLabel);
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
                      $"MaxExp={CharacterAttrCalc.GetRoleNextLevelExp(level):0.##} " +
                      $"TotalExp={CharacterAttrCalc.GetTotalExp(level)}");
        }

        /// <summary>
        /// 按输入的圣器类型、品级与等级计算并打印圣器数值与合成所需材料。
        /// </summary>
        private void OutputRelic()
        {
            int level = Mathf.Max(1, relicLevel);
            Item item = CreateRelicItem(relicType, relicQuality, level);

            Dictionary<EAttr, float> valueDic = RelicValueCalc.GetRelicValue(item);
            List<string> valueParts = new List<string>();
            foreach (var kv in valueDic)
            {
                valueParts.Add($"{kv.Key}={kv.Value:0.##}");
            }
            Debug.Log($"-- 圣器数值 {relicType} 品级={relicQuality} 等级={level} {string.Join(", ", valueParts)}");

            List<Item> needItems = RelicForgeCostCalc.GetNeedItems(item.ToRelicItem());
            Debug.Log($"---- 合成所需材料 {FormatItems(needItems)}");
        }

        /// <summary>
        /// 按输入的等级、品级与选中的类型计算合成材料, 打印一行各类型所需与一行合计。
        /// </summary>
        private void OutputRelicMaterials()
        {
            int level = Mathf.Max(1, materialLevel);
            List<ERelicType> types = GetSelectedRelicTypes();
            if (types.Count == 0)
            {
                Debug.LogWarning("-- 圣器合成材料: 未选中任何类型");
                return;
            }

            List<string> perTypeParts = new List<string>();
            List<Item> totalItems = new List<Item>();
            Dictionary<string, Item> totalItemDic = new Dictionary<string, Item>();
            foreach (ERelicType type in types)
            {
                Item relic = CreateRelicItem(type, materialQuality, level);
                List<Item> needItems = RelicForgeCostCalc.GetNeedItems(relic.ToRelicItem());
                perTypeParts.Add($"{type}[{FormatItems(needItems)}]");

                //同一种材料累加进合计
                foreach (Item needItem in needItems)
                {
                    if (totalItemDic.TryGetValue(needItem.ItemKey, out Item totalItem))
                    {
                        totalItem.num += needItem.num;
                        continue;
                    }

                    Item addItem = new Item(needItem.type, needItem.typeId, needItem.num, needItem.quality);
                    totalItemDic.Add(needItem.ItemKey, addItem);
                    totalItems.Add(addItem);
                }
            }

            Debug.Log($"-- 圣器合成材料 品级={materialQuality} 等级={level} " +
                      $"各自所需: {string.Join(" | ", perTypeParts)}");
            Debug.Log($"---- 圣器合成材料合计({types.Count}件): {FormatItems(totalItems)}");
        }

        /// <summary>
        /// 取当前勾选的圣器类型, 按类型枚举顺序返回。
        /// </summary>
        private List<ERelicType> GetSelectedRelicTypes()
        {
            List<ERelicType> types = new List<ERelicType>();
            foreach (ERelicType type in AllRelicTypes)
            {
                if (materialTypeSelected[type])
                {
                    types.Add(type);
                }
            }

            return types;
        }

        /// <summary>
        /// 初始化类型勾选状态, 默认全选(编辑器脚本重载后同样恢复全选)。
        /// </summary>
        private void EnsureMaterialTypesSelected()
        {
            if (materialTypeSelected.Count > 0)
            {
                return;
            }

            SetAllMaterialTypes(true);
        }

        /// <summary>
        /// 把全部圣器类型的勾选状态设置为指定值。
        /// </summary>
        private void SetAllMaterialTypes(bool selected)
        {
            foreach (ERelicType type in AllRelicTypes)
            {
                materialTypeSelected[type] = selected;
            }
        }

        /// <summary>
        /// 按类型、品级、等级拼出圣器物品, id 与运行时一致(示例: Relic/Weapon_1_10)。
        /// </summary>
        private static Item CreateRelicItem(ERelicType type, EQuality quality, int level)
        {
            string typeId = $"Relic/{type}_{(int)quality}_{level}";
            return new Item(ItemType.HolyRelic, typeId, level, quality);
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
        /// 绘制等级曲线 CSV 导出测试区。
        /// </summary>
        private void DrawLevelCurveSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("5. 导出升级曲线 CSV", EditorStyles.boldLabel);
            curveTargetLevel = EditorGUILayout.IntField("目标等级", curveTargetLevel);
            curveStartLevel = EditorGUILayout.IntField("起始等级", curveStartLevel);
            curveStartExp = EditorGUILayout.IntField("起始经验", curveStartExp);
            if (GUILayout.Button("导出升级曲线 CSV"))
            {
                OutputLevelCurve();
            }

            if (!string.IsNullOrEmpty(lastCurveCsvPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("最近导出", lastCurveCsvPath, EditorStyles.miniLabel);
                if (GUILayout.Button("定位文件", GUILayout.Width(60)))
                {
                    EditorUtility.RevealInFinder(lastCurveCsvPath);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 按输入的起始/目标等级与起始经验导出升级曲线 CSV(输出在 persistentDataPath 下)。
        /// </summary>
        private void OutputLevelCurve()
        {
            lastCurveCsvPath = LevelCurveDebug.DumpLevelingCurveToCsv(curveTargetLevel, curveStartLevel, curveStartExp);
            if (string.IsNullOrEmpty(lastCurveCsvPath))
            {
                //参数不合法时 DumpLevelingCurveToCsv 内部已经给出警告
                return;
            }

            Debug.Log($"-- 升级曲线已导出 {lastCurveCsvPath}");
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
