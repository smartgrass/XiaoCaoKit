using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace XiaoCao
{

    public class DelayShow : MonoBehaviour
    {
        public GameObject target;

        public UnityEvent unityEvent;

        public float delay = 0.5f;

        private void Awake()
        {
            XCTime.DelayRun(delay, SetActive).ToObservable();
        }

        void SetActive()
        {
            target.SetActive(true);
            unityEvent?.Invoke();
        }
    }
}
