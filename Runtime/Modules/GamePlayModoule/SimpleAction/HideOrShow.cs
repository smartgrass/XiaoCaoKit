using UnityEngine;


namespace XiaoCao
{

    public class HideOrShow : MonoBehaviour
    {
        public bool isActive = false;
        private void Awake()
        {
            gameObject.SetActive(isActive);
        }
    }
}
