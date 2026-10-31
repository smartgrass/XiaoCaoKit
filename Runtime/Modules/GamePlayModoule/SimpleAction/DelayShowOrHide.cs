using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace XiaoCao
{
    /// <summary>
    /// 延迟显示或隐藏目标节点: 延迟结束后按 <see cref="isHide"/> 切换 target 的激活状态, 再触发 unityEvent。
    /// </summary>
    public class DelayShowOrHide : MonoBehaviour
    {
        public GameObject target;

        [Tooltip("勾选后延迟隐藏目标, 不勾选则延迟显示")]
        public bool isHide;

        public UnityEvent unityEvent;

        public float delay = 0.5f;

        private void Awake()
        {
            XCTime.DelayRun(delay, ApplyActiveState).ToObservable();
        }

        private void ApplyActiveState()
        {
            target.SetActive(!isHide);
            unityEvent?.Invoke();
        }
    }
}
