using System;
using cfg;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XiaoCao;
using XiaoCaoKit;
using XiaoCaoKit.UI;

namespace XiaoCao.UI
{
    public class ModeBtn : NorBtn
    {
        public void SetMode(EPlayMode mode)
        {
            titleText.text = $"EPlayMode/{mode}".ToLocalizeStr();
        }
    }
}