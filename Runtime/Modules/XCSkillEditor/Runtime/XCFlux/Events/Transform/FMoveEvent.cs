using UnityEngine;

namespace Flux
{
    [FEvent("Transform/Tween Move", typeof(FTransformTrack))]
    public class FMoveEvent : FEvent
    {
        public Transform[] pathPoints; // 存储路径上的各个点
        public float[] arrivalTimes; // 存储每个点到达的时间
        public float totalDuration = 10.0f; // 整个路径的移动时间

        void Update()
        {
            float t = Mathf.Clamp01(Time.time / totalDuration); // 将时间映射到[0, 1]范围内

            GetPathPositionAndIndex(t, out Vector3 currentPosition, out int nextIndex);

            // 在这里可以使用 currentPosition，nextIndex 进行需要的操作
        }

        void GetPathPositionAndIndex(float normalizedTime, out Vector3 position, out int nextIndex)
        {
            float totalTime = normalizedTime * totalDuration;

            float accumulatedTime = 0.0f;
            nextIndex = 0;

            // 寻找当前时间所在的区间
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                accumulatedTime += arrivalTimes[i];

                if (accumulatedTime >= totalTime)
                {
                    nextIndex = i + 1;
                    break;
                }
            }

            // 计算插值百分比
            float tBetweenPoints = 1.0f;

            if (nextIndex > 0)
            {
                float timeBeforeCurrent = accumulatedTime - (totalTime - arrivalTimes[nextIndex - 1]);
                tBetweenPoints = timeBeforeCurrent / arrivalTimes[nextIndex - 1];
            }

            // 使用 Vector3.Lerp 计算当前位置
            position = Vector3.Lerp(pathPoints[nextIndex - 1].position, pathPoints[nextIndex].position, tBetweenPoints);
        }
    }
}
