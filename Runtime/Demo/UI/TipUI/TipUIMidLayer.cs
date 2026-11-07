using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 显示时用预制体生成一条提示条(TipUI)实例, 并挂到 Mid 层(UIMgr.midCanvas); 隐藏时销毁或停用该实例。
    /// 用在"界面显示时才需要提示条"的场景, 界面隐藏后 Mid 层不会留下多余节点。
    /// </summary>
    public class TipUIMidLayer : MonoBehaviour
    {
        [Label("提示条预制体")]
        [Tooltip("TipUI 预制体, 显示时生成实例并挂到 Mid 层")]
        public TipUI tipPrefab;

        [Label("隐藏时销毁实例")]
        [Tooltip("关闭后隐藏只停用实例, 下次显示直接复用")]
        public bool destroyOnHide = true;

        /// <summary>当前生成的提示条实例, 未显示时为 null</summary>
        public TipUI Tip { get; private set; }

        private void OnEnable()
        {
            Show();
        }

        private void OnDisable()
        {
            Hide();
        }

        /// <summary>生成(或复用)提示条实例, 挂到 Mid 层最上方, 并播放一次展开动画</summary>
        [Button("显示", enabledMode: EButtonEnableMode.Playmode)]
        public void Show()
        {
            //编辑器里打开预制体时不生成实例, 免得往场景里塞对象
            if (!Application.isPlaying)
            {
                return;
            }

            if (!UIMgr.HasInst)
            {
                Debug.LogWarning("UIMgr 还没创建, 无法把提示条挂到 Mid 层", this);
                return;
            }

            if (Tip == null)
            {
                //worldPositionStays 传 false, 保留预制体上摆好的锚点、尺寸与局部坐标
                Transform mid = UIMgr.Inst.midCanvas.transform;
                Tip = Instantiate(tipPrefab, mid, false);
                Tip.transform.SetAsLastSibling();
            }

            Tip.gameObject.SetActive(true);
            //挂到 Mid 层后立即播一次展开动画, 避免提示条凭空出现
            Tip.PlayPop();
        }

        /// <summary>隐藏提示条: 默认销毁实例, 关闭 destroyOnHide 时只停用以待复用</summary>
        [Button("隐藏", enabledMode: EButtonEnableMode.Playmode)]
        public void Hide()
        {
            if (Tip == null)
            {
                return;
            }

            if (destroyOnHide)
            {
                Destroy(Tip.gameObject);
                Tip = null;
                return;
            }

            Tip.gameObject.SetActive(false);
        }
    }
}
