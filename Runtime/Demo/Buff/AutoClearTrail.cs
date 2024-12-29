using UnityEngine;

namespace XiaoCao.Buff
{
    public class AutoClearTrail : MonoBehaviour
    {
        public ParticleSystem[] trailPs;
        private void OnDisable()
        {
            foreach (ParticleSystem p in trailPs)
            {
                p.Clear();
            }
        }
    }

}
