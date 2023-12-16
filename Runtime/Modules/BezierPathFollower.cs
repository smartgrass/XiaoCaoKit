using NaughtyAttributes;
using UnityEngine;

public class BezierPathFollower : MonoBehaviour
{
    public Transform targetObeject;



    //n
    public Vector3[] controlPoints; // 存储贝塞尔曲线上的控制点
    //n-1
    public Vector3[] handlePonts;
    //n-1
    public float[] arrivalTimes; // 存储每个点到达的时间



    [Button()]
    void GetPoints()
    {
        //controlPoints = transform.GetComponentsInChildren<Transform>();

        //int n = controlPoints.Length;
        //int n1 = n - 1;

        //arrivalTimes = new float[n1]; // 增加1
        //// 平均分配到达时间
        //for (int i = 0; i < n1; i++)
        //{
        //    arrivalTimes[i] = (i + 1) * (1f / (n1));
        //}

        //handlePonts = new Vector3[n1];
        //for (int i = 0; i < n1; i++)
        //{
        //    handlePonts[i] = (controlPoints[i].position + controlPoints[i + 1].position) / 2;
        //}

    }

    //void OnTimeChange()
    //{
    //    Vector3 currentPosition = GetBezierPathPositionAndIndex(t, out int nextIndex);
    //    targetObeject.transform.position = currentPosition;
    //}

    public Vector3 GetBezierPathPositionAndIndex(float normalizedTime, out int timeIndex)
    {
        int timeCount = arrivalTimes.Length;
        timeIndex = 0;
        // 寻找当前时间所在的区间
        for (int i = 0; i < timeCount; i++)
        {
            if (arrivalTimes[i] >= normalizedTime)
            {
                timeIndex = i;
                break;
            }
        }


        // 计算插值百分比
        float tBetweenPoints = 0.0f;
        float curTimeLen = 0.0f;
        float curTime = 0;

        if (timeIndex == 0)
        {
            curTimeLen = arrivalTimes[0];
            curTime = normalizedTime;
        }
        else
        {
            float lastTime = arrivalTimes[timeIndex - 1];
            curTimeLen = arrivalTimes[timeIndex] - lastTime;
            if (curTimeLen == 0)
            {
                return controlPoints[timeIndex + 1];
            }

            curTime = normalizedTime - lastTime;
        }
        tBetweenPoints = curTime / curTimeLen;

        return MathTool.GetBezierPoint2(controlPoints[timeIndex], controlPoints[timeIndex + 1], handlePonts[timeIndex], tBetweenPoints);
    }

}
