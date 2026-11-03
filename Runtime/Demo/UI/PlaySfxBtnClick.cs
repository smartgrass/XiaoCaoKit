using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    /// <summary>
    /// 按钮点击音效: 挂在带 Button 的节点上, Inspector 里绑好按钮与音效(可用右键 AutoBind),
    /// 按钮被点击时播放一次音效。其它脚本也能直接调用 <see cref="PlayClickSfx"/> 手动触发。
    /// </summary>
    public class PlaySfxBtnClick : MonoBehaviour
    {
        [Tooltip("被监听的按钮")]
        public Button btn;

        [Tooltip("点击时播放的音效, 直接从 Assets/_Res/Audio 下拖过来")]
        public AudioClip sfx;

        [Tooltip("音量 0~1")]
        [Range(0f, 1f)]
        public float volume = 1f;

        private void OnEnable()
        {
            btn.onClick.AddListener(PlayClickSfx);
        }

        private void OnDisable()
        {
            btn.onClick.RemoveListener(PlayClickSfx);
        }

        /// <summary>播放点击音效</summary>
        public void PlayClickSfx()
        {
            SoundMgr.Inst.PlayClip(sfx, volume);
        }
    }
}
