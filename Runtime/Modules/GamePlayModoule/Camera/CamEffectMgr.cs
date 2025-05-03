using UnityEngine;

namespace XiaoCao
{
    public class CamEffectMgr : MonoBehaviour
    {
        public static CamEffectMgr Inst;

        private ICamShake camShake;

        private void Awake()
        {
            Inst = this;
            camShake = postObjects[(int)CameraEffect.CamShake - 1].GetComponent<ICamShake>();
        }

        public GameObject[] postObjects;

        private CameraEffect curEffect = CameraEffect.None;

        public void SetOpenEffect(CameraEffect effect, bool isOn = true)
        {
            if (postObjects.Length <= (int)effect - 1)
            {
                Debug.LogError($"--- no postObjects {effect}");
                return;
            }

            //CloseEffect all


            if (effect != CameraEffect.None)
            {
                postObjects[(int)effect - 1].SetActive(isOn);
            }
            curEffect = effect;
        }

        public void CloseAllEffect()
        {
            //    if (curEffect != CameraEffect.None)
            //    {
            //        postObjects[(int)curEffect - 1].SetActive(false);
            //    }
        }

        public void CamShakeEffect(int shakeLevel)
        {
            Debug.Log($"--- shakeLevel {shakeLevel} ");
            camShake.SetLevel(shakeLevel);
        }

        public enum CameraEffect
        {
            None = 0,
            TimeStop = 1,
            CamShake = 2
        }

    }


    public interface ICamShake
    {
        ///<see cref="PostProcessCamShake.SetLevel(int)"/>
        void SetLevel(int level);
    }

}


