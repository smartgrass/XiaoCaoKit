using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装备强化金币消耗计算
/// </summary>
public class EnhanceCostCalculator
{
    // ==========【策划可调顶层参数，就这5个】==========
    private const int BaseValue = 100;
    private const double ExponentFactor = 1.08;
    private const int Stage1Max = 10;    // 1~10指数段
    private const int DeltaStage2 = 60;  // 11~20线性增量
    private const int DeltaStage3 = 100; // 21~30线性增量
    private const int MaxEnhanceCount = 30;
    // ================================================

    /// <summary>
    /// 获取单次强化消耗
    /// n:强化序号，1=0→1，30=29→30
    /// </summary>
    public static int GetSingleEnhanceCost(int n)
    {
        if (n < 1 || n > MaxEnhanceCount)
        {
            throw new ArgumentOutOfRangeException(nameof(n), $"n必须是1~{MaxEnhanceCount}");
        }

        if (n <= Stage1Max)
        {
            // 阶段1：指数增长 1~10
            double val = BaseValue * Math.Pow(ExponentFactor, n - 1);
            return (int)Math.Round(val);
        }
        else if (n <= Stage1Max + 10)
        {
            // 阶段2：11~20，线性
            int costStage1End = GetSingleEnhanceCost(Stage1Max);
            return costStage1End + (n - Stage1Max) * DeltaStage2;
        }
        else
        {
            // 阶段3：21~30，线性
            int costStage2End = GetSingleEnhanceCost(Stage1Max + 10);
            return costStage2End + (n - (Stage1Max + 10)) * DeltaStage3;
        }
    }

    /// <summary>
    /// 获取从0强化到目标等级，累计总消耗
    /// </summary>
    public static int GetTotalCostToLevel(int targetN)
    {
        if (targetN < 1 || targetN > MaxEnhanceCount)
            throw new ArgumentOutOfRangeException(nameof(targetN));

        int sum = 0;
        for (int i = 1; i <= targetN; i++)
        {
            sum += GetSingleEnhanceCost(i);
        }
        return sum;
    }

    public struct EnhanceRow
    {
        public int EnhanceIndex;
        public int SingleCost;
        public int TotalCost;
    }

    /// <summary>
    /// 批量生成全部强化表，用于导出CSV/调试
    /// </summary>
    public static List<EnhanceRow> GetAllEnhanceTable()
    {
        var list = new List<EnhanceRow>();
        int total = 0;
        for (int n = 1; n <= MaxEnhanceCount; n++)
        {
            int single = GetSingleEnhanceCost(n);
            total += single;
            list.Add(new EnhanceRow
            {
                EnhanceIndex = n,
                SingleCost = single,
                TotalCost = total
            });
        }
        return list;
    }

    // 控制台打印全部数据，测试用
    public static void PrintAllTable()
    {
        var table = GetAllEnhanceTable();
        Debug.Log($"{"序号",-6}{"单次",-8}{"累计",-8}");
        foreach (var row in table)
        {
            Debug.Log($"{row.EnhanceIndex,-6}{row.SingleCost,-8}{row.TotalCost,-8}");
        }
    }
}
