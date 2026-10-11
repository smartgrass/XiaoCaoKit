using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 等级/关卡经验曲线调试工具
/// 汇总了首通奖励、普通通关奖励、升级消耗三套规则，
/// 并提供逐关推演导出 CSV 的调试方法。
/// </summary>
public static class LevelCurveDebug
{
    // ==================== 规则1：升级所需经验 ====================
    // 从 lv 升到 lv+1 所需经验
    public static int GetRoleNextLevelExp(int lv)
    {
        float k = lv / 10f;
        float val = Mathf.Pow(2, k);
        return (int)(75 * val);
    }

    // ==================== 规则2：普通通关奖励（重复/扫荡） ====================
    // addExp = 20，chapterBaseExp = 0，5 倍数大关翻倍
    public static int CalcNormalPassExp(int levelIndex)
    {
        const int addExp = 20;
        int group = (levelIndex + 4) / 5;          // 5关一组
        int baseGroupExp = addExp * group;         // chapterBaseExp = 0
        bool isGroupLast = (levelIndex % 5) == 0;
        return isGroupLast ? baseGroupExp * 2 : baseGroupExp;
    }

    // ==================== 规则3：首通奖励 ====================
    // 1~4 关：特殊固定值；5~30 关：100（5倍数关×2）；>=31：随 group 增长（5倍数关×2）
    public static int CalcFirstPassExp(int levelIndex)
    {
        int group = (levelIndex + 4) / 5;
        float baseFirst;

        if (levelIndex >= 1 && levelIndex <= 4)
        {
            // 1~4 关特殊首通
            switch (levelIndex)
            {
                case 1: baseFirst = 80;  break;
                case 2: baseFirst = 90;  break;
                case 3: baseFirst = 100; break;
                case 4: baseFirst = 100; break;
                default: baseFirst = 100; break;
            }
        }
        else if (levelIndex >= 5 && levelIndex <= 30)
        {
            baseFirst = 100;
        }
        else
        {
            // 方案3：绑定 group，随关卡增长
            baseFirst = 100f * (1 + (group - 6) * 0.35f);
        }

        if (levelIndex % 5 == 0)
            baseFirst *= 2;

        return (int)Math.Round(baseFirst);
    }

    /// <summary>单关首通总经验 = 首通奖励 + 普通通关奖励</summary>
    public static int CalcTotalFirstPassExp(int levelIndex)
        => CalcFirstPassExp(levelIndex) + CalcNormalPassExp(levelIndex);

    // ==================== 调试方法：逐关推演并导出 CSV ====================
    /// <summary>
    /// 从起始等级/经验出发，从第 1 关起逐关推演，直到达到目标等级，
    /// 将每一关的通关结果写入 CSV 文件。
    /// </summary>
    /// <param name="targetLevel">目标等级（达到或超过即停止）</param>
    /// <param name="startLevel">起始等级，默认 1</param>
    /// <param name="startExp">起始经验，默认 0</param>
    /// <returns>CSV 文件完整路径</returns>
    public static string DumpLevelingCurveToCsv(int targetLevel, int startLevel = 1, int startExp = 0)
    {
        if (targetLevel <= startLevel)
        {
            Debug.LogWarning($"[LevelCurveDebug] targetLevel({targetLevel}) 必须大于 startLevel({startLevel})，已跳过。");
            return null;
        }

        int level = startLevel;
        int exp = startExp;
        int levelIndex = 1;

        var sb = new StringBuilder();
        sb.AppendLine("levelIndex,firstPassExp,normalPassExp,totalExp,levelAfter,remainExp");

        while (level < targetLevel)
        {
            int firstExp = CalcFirstPassExp(levelIndex);
            int normalExp = CalcNormalPassExp(levelIndex);
            int totalExp = firstExp + normalExp;

            exp += totalExp;

            // 连续结算升级，直到无法再升级
            while (exp >= GetRoleNextLevelExp(level))
            {
                exp -= GetRoleNextLevelExp(level);
                level++;
            }

            sb.AppendLine($"{levelIndex},{firstExp},{normalExp},{totalExp},{level},{exp}");
            levelIndex++;

            // 安全上限，防止配置异常导致死循环
            if (levelIndex > 10000)
            {
                Debug.LogError("[LevelCurveDebug] 关卡数超过 10000，疑似目标等级不可达，已强制终止。");
                break;
            }
        }

        string path = Path.Combine(Application.persistentDataPath, "leveling_curve.csv");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[LevelCurveDebug] 已导出 {levelIndex - 1} 关数据 → {path}");
        return path;
    }

    // ==================== 调试示例 ====================
    // 在任意脚本里调用：
    //   string p = LevelCurveDebug.DumpLevelingCurveToCsv(50);            // 从头推到 50 级
    //   string p2 = LevelCurveDebug.DumpLevelingCurveToCsv(30, 10, 500);  // 从 10 级 500 经验推到 30 级
    //
    // 注意：起始等级 > 1 时，仅改变角色初始状态，关卡仍从第 1 关开始计，
    // 因此会重复计入此前关卡的奖励。若需要从指定关卡续推，请自行调整 levelIndex 起点。
}
//（注：内容由AI生成）
