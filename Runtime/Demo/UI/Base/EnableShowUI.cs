using System;
using UnityEngine;

namespace XiaoCao.UI
{
    /// <summary>
    /// 简易的ui:显示时自动刷新
    /// </summary>
    public abstract class EnableShowUI : MonoBehaviour
    {
        public virtual void OnEnable()
        {
            UpdateUI();
        }

        public virtual void UpdateUI()
        {
        }
    }
}