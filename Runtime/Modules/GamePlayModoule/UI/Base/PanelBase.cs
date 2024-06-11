using UnityEngine.UI;
namespace XiaoCao
{
    public abstract class PanelBase : ViewBase
    {
        public Button sureBtn;

        public Button closeBtn;

        public virtual bool StopPlayerControl => true;
        public bool IsInited { get; set; }

        public bool IsShowing;

        public virtual UIPanelType panelType { get; }

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
}
