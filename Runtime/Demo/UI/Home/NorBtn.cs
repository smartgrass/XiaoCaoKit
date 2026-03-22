
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    public class NorBtn : MonoBehaviour
    {
        public TMP_Text titleText;
        public Button btn;
        public int curIndex;

        public Action onClick;


        private void Awake()
        {
            btn.onClick.AddListener(() => { onClick?.Invoke(); });
        }
    }
}
