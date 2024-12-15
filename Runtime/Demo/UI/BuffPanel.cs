using UnityEngine;

namespace XiaoCao
{
    public class BuffPanel : SubPanel
    {
        public BuffPanelView view;
        public override void Init()
        {
            view = gameObject.GetComponent<BuffPanelView>();
            view.Init();
        }

        public override void RefreshUI()
        {
            view.RefreshUI();
        }

    }
}