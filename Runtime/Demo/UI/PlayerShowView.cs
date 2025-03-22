using UnityEngine;
using UnityEngine.PlayerLoop;

namespace XiaoCao
{
    public class PlayerShowView : MonoBehaviour
    {
        public PlayerShowPanel panel;

        public void Init()
        {

        }
        public void RefreshUI()
        {

        }

        void Update()
        {
            if (HotFlags.PlayerAttrChange)
            {
                panel.RefreshUI();
                HotFlags.PlayerAttrChange =false;
            }
        }

    }
}