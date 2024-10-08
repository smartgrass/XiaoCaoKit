using UnityEngine.UI;
namespace XiaoCao
{
    public abstract class PanelBase : ViewBase
    {
        public Button closeBtn;

        public virtual bool StopPlayerControl => true;

        public bool IsInited { get; set; }

        public bool IsShowing;

        public virtual UIPanelType panelType { get; }

        public virtual void Init()
        {
            closeBtn.onClick.AddListener(OnCloseBtnClick);
        }

        public virtual void OnCloseBtnClick()
        {

        }


        /* 示例代码
         
        public override void Init()
        {
            base.Init();
            IsInited = true;
        }
        #region 常规
        public override void OnCloseBtnClick()
        {
            Hide();
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
        #endregion
        */
    }
}
