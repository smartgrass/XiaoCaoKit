using UnityEngine;


namespace XiaoCao
{
    public class HideObjects : MonoBehaviour
    {
        public bool isActive = false;

        public GameObject[] objects;

        private void Awake()
        {
            foreach (GameObject obj in objects)
            {
                obj.SetActive(isActive);
            }
        }
    }
}
