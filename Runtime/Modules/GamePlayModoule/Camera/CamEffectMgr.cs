using UnityEngine;

namespace XiaoCao
{
    public class CamEffectMgr : MonoBehaviour
    {
        public static CamEffectMgr Inst;
        private void Awake()
        {
            Inst = this;
        }

        public GameObject[] postObjects;

        private CameraEffect curEffect = CameraEffect.None;

        public void OpenEffect(CameraEffect effect)
        {
            if (postObjects.Length <= (int)effect - 1)
            {
                Debug.LogError($"--- no postObjects {effect}");
                return;
            }

            CloseEffect();
            if (effect != CameraEffect.None)
            {
                postObjects[(int)effect - 1].SetActive(true);
            }
            curEffect = effect;
        }

        void CloseEffect()
        {
            if (curEffect != CameraEffect.None)
            {
                postObjects[(int)curEffect - 1].SetActive(false);
            }
        }


        public enum CameraEffect
        {
            None = 0,
            TimeStop = 1,
        }

    }

}


