
using System;
using UnityEngine.UI;

namespace XiaoCao
{
    public abstract class StandardPanel<T> : PanelBase where T : IUIData
    {
        /// <summary>
        /// 启动前传递数据
        /// </summary>
        public T data { get; set; }

        /* 示例代码
        public override void Init()
        {
            base.Init();

        }
        public override void Hide()
        {
            gameObject.SetActive(false);
            IsShowing = false;
            UIMgr.Inst.HideView(panelType);
        }

        public override void Show()
        {
            if (!IsInited)
            {
                Init();
            }
            IsShowing = true;
            gameObject.SetActive(true);
        }

        */
    }


    public interface IUIData { }

    public class UIData0 : IUIData
    {
        public string strMsg;
        public int numMsg;
    }
}
