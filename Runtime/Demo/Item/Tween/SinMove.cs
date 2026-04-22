using UnityEngine;

/// <summary>
/// Y轴正弦循环上下漂浮动画
/// 适配3D物体、世界空间UI、场景道具
/// </summary>
public class SinMove : MonoBehaviour
{
    [Header("漂浮幅度")] public float floatAmplitude = 0.2f; // Y轴上下浮动最大距离
    [Header("漂浮速度")] public float floatSpeed = 2f; // 动画循环快慢
    [Header("初始相位偏移")] public float phaseOffset = 0f; // 错开多个道具动画，避免全部同步跳动

    private Vector3 startLocalPos; // 物体初始本地位置

    void Start()
    {
        // 记录物体一开始的位置，以原点做浮动基准
        startLocalPos = transform.localPosition;
    }

    void Update()
    {
        // 正弦函数Mathf.Sin，周期循环-1~1，完美柔和Y轴往复动画
        float sinY = Mathf.Sin(Time.time * floatSpeed + phaseOffset) * floatAmplitude;

        // 只修改Y轴，X/Z位置完全不变
        transform.localPosition = new Vector3(
            startLocalPos.x,
            startLocalPos.y + sinY,
            startLocalPos.z
        );
    }
}