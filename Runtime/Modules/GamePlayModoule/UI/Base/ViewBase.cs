using UnityEngine;

namespace XiaoCao
{
    public abstract class ViewBase : MonoBehaviour
    {
        public abstract void Show(IUIData data = null);

        public abstract void Hide();
    }
}
