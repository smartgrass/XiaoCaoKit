
using System;
using UnityEngine.UI;

namespace XiaoCao
{
    public abstract class StandardPanel<T> : ViewBase where T : IUIData
    {

        /* 示例代码
        public override void Init()
        {
            base.Init();

        }
        public override void Show()
        {
            if (!IsInited)
            {
                Init();
            }

            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        */

        public Button sureBtn;

        public Button closeBtn;

        public bool IsInited { get; set; }
        /// <summary>
        /// 启动前传递数据
        /// </summary>
        public T data { get; set; }

        public virtual void Init()
        {
            sureBtn.onClick.AddListener(OnSureBtnClick);
            closeBtn.onClick.AddListener(OnCloseBtnClick);
        }

        public virtual void OnCloseBtnClick()
        {

        }

        public virtual void OnSureBtnClick()
        {

        }

    }


    public interface IUIData { }

    public class UIData0: IUIData
    {
        public string strMsg;
        public int numMsg;
    }

}
