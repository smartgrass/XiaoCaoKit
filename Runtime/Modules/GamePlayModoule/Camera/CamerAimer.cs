using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UIElements;

namespace XiaoCao
{
    public class CamerAimer : MonoBehaviour
    {
        public List<Transform> list = new List<Transform> { null, null };

        public float weight = 1;

        public float smoothTime = 0.3f;

        public float forwardLen = 5;

        private Vector3 _tempVec;


        public void SetMainWeight(float weight)
        {
            this.weight = weight;
        }

        public void SetAim(Transform tf, int index = 1)
        {
            list[index] = tf;
        }



        private void FixedUpdate()
        {
            Vector3 lastPos = transform.position;

            Vector3 targetPos = CalculateWeightedCenter(list, weight);

            transform.position = Vector3.SmoothDamp(lastPos, targetPos, ref _tempVec, smoothTime);
        }

        public Vector3 CalculateWeightedCenter(List<Transform> list, float weight)
        {
            if (null == list[0])
            {
                return transform.position;
            }

            Vector3 weightedCenter = list[0].position * weight + list[0].forward.ToY0() * forwardLen;

            Vector3 otherPointAddd = Vector3.zero;
            int otherCount = 0;

            for (int i = 1; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    otherPointAddd += list[i].position;
                    otherCount++;
                }
            }
            if (otherCount == 0)
            {
                return transform.position;
            }

            weightedCenter = weightedCenter + otherPointAddd * GetWeight(otherCount, weight);
            return weightedCenter;

        }

        public float GetWeight(int count, float mainWeight)
        {
            //main 最大为1
            float otherWeight = 1 - weight;

            return otherWeight / count;
        }

    }

}


