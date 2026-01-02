using UnityEngine;

namespace XiaoCao.UI
{
    public abstract class HomePanelBase : MonoBehaviour
    {
        public void Back()
        {
            HomeHud.Inst.SwitchPanel(EHomePanel.MainPanel);
        }
    }
}