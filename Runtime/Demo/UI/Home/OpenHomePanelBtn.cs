using System;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCaoKit
{
    public class OpenHomePanelBtn : MonoBehaviour
    {
        public EHomePanel panel;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            HomeHud.Inst.SwitchPanel(panel);
        }
    }
}