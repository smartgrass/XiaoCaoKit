using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCaoKit.UI
{
    public class UIStateChangeColor : UIStateChange
    {
        public List<UIColor> uiColorGroups = new List<UIColor>();


        public override void ApplyState()
        {
            base.ApplyState();
            foreach (var uiColor in uiColorGroups)
            {
                uiColor.img.color = uiColor.colors[state];
            }
        }
    }

    [System.Serializable]
    public class UIColor
    {
        public Image img;
        public Color[] colors;
    }
}