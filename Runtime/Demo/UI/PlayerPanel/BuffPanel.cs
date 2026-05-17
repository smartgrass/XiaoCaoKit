using UnityEngine;
using XiaoCao.UI;

namespace XiaoCao
{
    public class BuffPanel : SubPanel
    {
        public BuffView view;
        public override void Init()
        {
            view = gameObject.GetComponent<BuffView>();
        }

        public override void RefreshUI()
        {
            view.UpdateUI();
        }

    }
}