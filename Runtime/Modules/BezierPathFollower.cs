using NaughtyAttributes;
using UnityEngine;

public class BezierPathFollower : MonoBehaviour
{
    public Transform targetObeject;

    [Range(0,1)]
    [OnValueChanged(nameof(OnTChange))]
    public float t;
    
    public Transform[] controlPoints; // 存储贝塞尔曲线上的控制点
    public Vector3[] handlePonts;
    public float[] arrivalTimes; // 存储每个点到达的时间
    public float totalDuration = 10.0f; // 整个路径的移动时间


    [Button()]
    void GetPoints()
    {
        controlPoints = transform.GetComponentsInChildren<Transform>();
        arrivalTimes = new float[controlPoints.Length + 1]; // 增加1
        // 平均分配到达时间
        for (int i = 0; i < controlPoints.Length; i++)
        {
            arrivalTimes[i] = i * (1f / (controlPoints.Length - 1));
        }

        handlePonts = new Vector3[controlPoints.Length - 1];
        for (int i = 0; i < controlPoints.Length -1; i++)
        {
            handlePonts[i] = (controlPoints[i].position + controlPoints[i + 1].position)/2;
        }
        
        
        arrivalTimes[controlPoints.Length] = 1f; // 
    }

    void OnTChange()
    {
        GetBezierPathPositionAndIndex(t, out Vector3 currentPosition, out int nextIndex);
        targetObeject.transform.position = currentPosition;
    }
    
    // void Update()
    // {
    //     float t = Mathf.Clamp01(Time.time / totalDuration); // 将时间映射到[0, 1]范围内
    //
    //     GetBezierPathPositionAndIndex(t, out Vector3 currentPosition, out int nextIndex);
    //
    //     targetObeject.transform.position = currentPosition;
    //     // 在这里可以使用 currentPosition，nextIndex 进行需要的操作
    // }

    void GetBezierPathPositionAndIndex(float normalizedTime, out Vector3 position, out int nextIndex)
    {
        float totalTime = normalizedTime ;

        float accumulatedTime = 0.0f;
        nextIndex = 0;

        // 寻找当前时间所在的区间
        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            accumulatedTime += arrivalTimes[i];

            if (accumulatedTime >= totalTime)
            {
                nextIndex = i + 1;
                break;
            }
        }
        
        // 计算插值百分比
        float tBetweenPoints = 0.0f;

        if (nextIndex > 0)
        {
            float timeBeforeCurrent = accumulatedTime - (totalTime - arrivalTimes[nextIndex - 1]);
            tBetweenPoints = timeBeforeCurrent / arrivalTimes[nextIndex - 1];
        }

        
        
        
        int startIndex = Mathf.Max(0, nextIndex - 2);
        int endIndex = Mathf.Min(controlPoints.Length - 1, nextIndex);

        
        position = MathTool.GetBezierPoint2(controlPoints[startIndex].position, controlPoints[startIndex + 1].position, handlePonts[startIndex], tBetweenPoints);
        Debug.Log($"startIndex: {startIndex}, endIndex: {endIndex}, tBetweenPoints: {tBetweenPoints}");
        Debug.Log(
            $"p0: {controlPoints[startIndex].position}, p1: {controlPoints[startIndex + 1].position}, p2: {controlPoints[endIndex].position}");
    }
    
}
