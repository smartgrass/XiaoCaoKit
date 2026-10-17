using System;
using NaughtyAttributes;
using System.Collections.Generic;
using GG.Extensions;
using UnityEngine;
using XiaoCao;
using XiaoCao.Map;

public class Test_WallLayout : MonoBehaviour
{
    public Test_WallCell prefab;

    public GameObject PrefabA => prefab.ABodyPrefab;
    public GameObject PrefabB => prefab.BPrefab;
    public float ABodyWidth => prefab.ABodyWidth;
    public float AACross => prefab.AACross;
    public float BWidth => prefab.BWidth;
    public float ALeftOffset => prefab.ALeftOffset;
    public float BValidWidth => prefab.BValidWidth;
    public int ARepeatCount => prefab.ARepeatCount;
    public float LenFractionThreshold => prefab.lenFractionThreshold;

    //围墙/栏杆的结构: 每 ARepeatCount 段 A(墙段) 接 1 个连接点 B, 即 A A B A A B ...
    //相邻两个 A 之间有 AACross 的重叠, 所以每段 A 实际推进 = ABodyWidth - AACross.
    //锚点位置不一样: A的锚点靠左(按左边缘对齐, 用 ALeftOffset 修正), B的锚点居中(直接按中心对齐).
    //B 的有效宽度 BValidWidth(手填 0~1): 1 = 两个A之间留出B整宽; 0 = 两个A紧挨着, B压在中间.
    //len 表示要摆多少段 A(允许小数, "按距离生成"会把反推出的段数原样写进来); 也可以直接填"距离"(世界长度), 点"按距离生成"反推 len

    /// <summary>要摆多少段 A (每 ARepeatCount 段 A 后面接 1 个连接点 B), 允许小数, 实际摆放按格子上的段数进位阈值取整</summary>
    public float len = 5f;

    /// <summary>整条墙的世界长度, 配 "按距离生成" 用</summary>
    public float distance = 20f;

    /// <summary>折算用: 1 段 A 平均占的长度(把每组里的 B 摊到 A 上) = 每段A的推进 + (重叠 + B有效宽)/每组A数, 节点本地单位</summary>
    private float LengthPerA => Mathf.Max(0.0001f, ABodyWidth - AACross) +
                                (AACross + BWidth * BValidWidth) / Mathf.Max(1, ARepeatCount);

    /// <summary>节点自身的横向缩放(距离按世界长度算, 这里折算回本地单位)</summary>
    private float WorldScaleX => Mathf.Max(0.0001f, transform.lossyScale.x);

    [Button("生成")]
    public void Gen()
    {
        // 按 len 直接拼: 用零件本身的 AA 重叠
        GenInternal(AACross, GetPieceCount(len));
    }

    /// <summary>
    /// 按距离反推 len 再生成:
    /// 1) 反推出的段数(带小数)原样记回 len, 真正要摆几段按格子上(prefab)的段数进位阈值取整:
    ///    小数部分超过阈值就进位(多摆一段、重叠压紧一点凑距离), 否则舍掉小数(比如 7.8 -> 7 段);
    ///    若舍掉小数后"重叠全削掉"也够不到目标长度(段数少的短段常这样), 就补一段, 免得留缝;
    /// 2) 段数定下来后, 差的那截长度平摊到"能吸收的地方": 各处的 AA 接缝 + 整条的两个端头, 共 (接缝数 + 2) 份;
    ///    两个端头各拿走一份 —— 表现成整条从两头往中间收缩(起点往右挪一份, 终点往左挪一份);
    ///    剩下的由 AA 重叠吸收(重叠削小整条被拉长, 压紧整条变短), 让这条墙的跨度刚好凑到目标距离.
    /// </summary>
    [Button("按距离生成")]
    public void GenByDistance()
    {
        float targetWorld = distance;
        float targetLocal = distance / WorldScaleX;
        len = targetLocal / LengthPerA;         // 反推出的段数, 保留小数方便看差多少段

        int count = GetPieceCount(len);
        // 0.0001 是浮点误差余量: 距离刚好等于能凑出来的长度时不该白白多摆一段
        if (RunLength(count, 0f) < targetLocal - 0.0001f)
        {
            count++;                            // 最松(重叠=0)也够不到, 只能多摆一段再压紧
        }

        int bCount = GetBCount(count);
        int jointCount = count - 1 - bCount;            // 被 B 占掉的位置不是 A-A 接缝, 不能重叠
        float baseLength = RunLength(count, 0f);        // 重叠全削掉(重叠=0)时整条的长度

        // 两端也算两个平摊单位: 长度差按 (接缝数 + 2) 份摊, 两端各拿走一份
        float endInset = jointCount > 0 ? (baseLength - targetLocal) / (jointCount + 2) : 0f;
        // 扣掉两端内缩后剩下的长度差, 反解成各处 A-A 接缝的重叠值
        // 整条长度 = 段数*A宽 + 连接点个数*B有效宽 - 可重叠接缝个数*重叠值
        float cross = jointCount > 0
            ? (baseLength - (targetLocal - endInset * 2f)) / jointCount
            : AACross;                              // ARepeatCount=1 时没有 A-A 接缝, 重叠没法改
        // 下限 0 = 最多把重叠削到 A 首尾相接; 上限 = 两块 A 咬合不超过各自一半
        cross = Mathf.Clamp(cross, 0f, ABodyWidth * 0.5f);
        // 夹完之后按实际用的重叠回算两端内缩: 整条对称地落在目标跨度里, 两头留一样多的量
        endInset = Mathf.Max(0f, (targetLocal - (baseLength - jointCount * cross)) * 0.5f);

        GenInternal(cross, count, endInset);

        Debug.Log($"-- 按距离生成: 段数 {count}(len {len:F2}), 重叠 {cross:F3}(原 {AACross:F3}), 两端内缩 {endInset:F3}, " +
                  $"实际长度 {distance:F3}(目标 {targetWorld:F3})");
    }

    /// <summary>把带小数的段数取整: 小数部分超过阈值就进位, 否则舍掉小数</summary>
    private int GetPieceCount(float value)
    {
        int floorValue = Mathf.Max(1, Mathf.FloorToInt(value));
        // 0.0001 是浮点误差余量: len 正好是 7.8 这种值时不该因为误差进位
        float frac = value - floorValue;
        return frac > LenFractionThreshold + 0.0001f ? floorValue + 1 : floorValue;
    }

    /// <summary>count 段 A 在给定重叠下的整条长度(节点本地单位)</summary>
    private float RunLength(int count, float cross)
    {
        int bCount = GetBCount(count);
        // A 之间的重叠只发生在没被 B 占掉的那些接缝上
        return count * ABodyWidth + bCount * BWidth * BValidWidth -
               (count - 1 - bCount) * cross;
    }

    /// <summary>整条墙会插入多少个连接点 B: 每 ARepeatCount 段 A 后面接一个, 末尾不足一组的没有 B</summary>
    private int GetBCount(int aCount)
    {
        int fullGroup = aCount / ARepeatCount;
        return aCount % ARepeatCount == 0 ? fullGroup - 1 : fullGroup;
    }

    /// <summary>按给定的 AA 重叠拼接 count 段 A(两个端头各内缩 endInset 再起摆), 拼完把整条占的跨度写回 distance</summary>
    private void GenInternal(float cross, int count, float endInset = 0f)
    {
        transform.DestroyChildren();

        // 结构: A A B A A B ... , 每 ARepeatCount 段 A 接 1 个 B
        // A 的锚点在左侧(左边缘), 所以 cursor 表示"下一段A左边缘"的位置; 从 endInset 起摆 = 整条从两头往中间收缩
        float aStep = Mathf.Max(0.0001f, ABodyWidth - cross);   // 相邻A的重叠算进来后的推进
        float cursor = endInset;
        int placedCount = 0; // 已放了多少段 A

        while (placedCount < count)
        {
            for (int i = 0; i < ARepeatCount && placedCount < count; i++)
            {
                GameObject a = BuildingTool.EditorInstancePrefab(PrefabA, transform);
                // 按"左边缘"对齐(A 靠左锚点时 ALeftOffset≈0)
                a.transform.localPosition = new Vector3(cursor - ALeftOffset, 0, 0);
                cursor += aStep;
                placedCount++;
            }

            // 还没到长度就接一个连接点 B: 贴在最后一段A的右边缘上 (最后一段A位置 + ABodyWidth = 游标 + 重叠值)
            if (placedCount < count)
            {
                float bLeft = cursor + cross;
                GameObject b = BuildingTool.EditorInstancePrefab(PrefabB, transform);
                // B 锚点在中间, 所以按中心对齐: 中心 = A右边缘 + 1/2 * B有效宽
                b.transform.localPosition = new Vector3(bLeft + BWidth * BValidWidth * 0.5f, 0, 0);
                // B 有效宽度决定下一组A往后挪多少(0=紧挨着, 1=留出B整宽)
                cursor += cross + BWidth * BValidWidth;
            }
        }

        // 生成完顺便按实际重叠把整条墙的长度写回 distance
        // 整条跨度 = 两端内缩 + (段数-1) 次推进 + 连接点个数*(重叠 + B有效宽) + 最后一段A自己的宽度
        float actualLocal = endInset * 2f + (count - 1) * aStep +
                            GetBCount(count) * (cross + BWidth * BValidWidth) + ABodyWidth;
        distance = actualLocal * WorldScaleX;
    }

    [Button]
    void AddBoxCollider()
    {
        BuildingTool.AddBoxCollider(transform);
    }
}
