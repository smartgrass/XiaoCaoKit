using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public class ChildPos : MonoBehaviour
    {
        public List<Vector3> posList = new List<Vector3>();

        [Button]
        public void SetChildPos()
        {
            int count = transform.childCount;
            for (int i = 0; i < posList.Count; i++)
            {
                if (i < count)
                {
                    transform.GetChild(i).position = posList[i];
                }
            }
            Debug.Log($"--- SetCurPos");
        }
        [Button]
        void GetCurPos()
        {
            int count = transform.childCount;
            posList.Clear();
            for (int i = 0; i < count; i++)
            {
                posList.Add(transform.GetChild(i).position);
            }
            Debug.Log($"--- GetCurPos");
        }

    }

}
