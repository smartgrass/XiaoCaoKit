using UnityEngine.UI;
namespace XiaoCao
{
    public abstract class PanelBase : ViewBase
    {
        public Button sureBtn;

        public Button closeBtn;
        public bool IsInited { get; set; }

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
