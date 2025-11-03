using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao.Map
{
    public class Test_WallCell : MonoBehaviour
    {
        [MiniBtn(nameof(GetChild))] public List<GameObject> prefabs = new List<GameObject>();

        public GameObject BodyPrefab => prefabs[0];
        public GameObject BodyPrefabB => prefabs[1];
        public GameObject NodePrefab => prefabs[2];
        
        public GameObject BodyPrefabC => prefabs[3];

        public float bodyWidth;

        public float nodeOffset;
        
        public float nodeWidth;

        private void GetChild()
        {
            if (transform.childCount < 4)
            {
                Debug.LogError($"-- 需要 4个子物体");
                return;
            }
            prefabs = new List<GameObject>();
            for (int i = 0; i < 4; i++)
            {
                var child = transform.GetChild(i).gameObject;
                prefabs.Add(child);
            }
        }

        [Button("计算")]
        public void Caculate()
        {
            if (prefabs.Count == 0)
            {
                GetChild();
            }

            //bodyWith = BodyPrefab 与 BodyPrefabB的距离
            Vector3 pos1 = BodyPrefab.transform.localPosition;
            Vector3 pos2 = BodyPrefabB.transform.localPosition;
            bodyWidth = Vector3.Distance(pos1, pos2);
            //nodeOffset = NodePrefab 与 BodyPrefabB的距离
            nodeOffset = Vector3.Distance(NodePrefab.transform.localPosition, pos2);
            
            //nodeWidth = NodePrefab 与 BodyPrefabc的距离
            nodeWidth = Vector3.Distance(NodePrefab.transform.localPosition, BodyPrefabC.transform.localPosition);
        }
    }
}