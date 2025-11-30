using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{
    public abstract class PanelBase : ViewBase
    {
        public abstract UIPanelType PanelType { get; }

        public virtual bool NeedUIData => false;

        public virtual bool IsShowing => gameObject.activeInHierarchy;

        public virtual bool HideEsc => true;
        
        //是否暂停角色操作
        public virtual bool StopPlayerControl => true;

        public virtual void InputKeyCode(KeyCode key)
        {
            
        }
    }

    public abstract class StandardPanel : PanelBase
    {
        public Button closeBtn;
        public bool IsInited { get; set; }
        
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