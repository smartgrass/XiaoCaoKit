using UnityEngine;

namespace XiaoCaoKit
{
    public abstract class HomePanelBase : MonoBehaviour
    {
        public void Back()
        {
            HomeHud.Inst.SwitchPanel(EHomePanel.MainPanel);
        }
    }
}