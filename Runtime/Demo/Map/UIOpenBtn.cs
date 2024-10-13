using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using Button = UnityEngine.UI.Button;

namespace XiaoCao
{
    /// <summary>
    ///<see cref="EventTriggerListener"/>
    /// </summary>
    public class UIOpenBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsHideSelf;

        public UIPanelType type;

        private void Start()
        {
            transform.GetComponent<Button>().onClick.AddListener(Open);
        }

        [Button]
        private void Open()
        {
            UIMgr.Inst.ShowView(type);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            BattleData.Current.UIEnter = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            BattleData.Current.UIEnter = false;
        }

        //public void OnBtnDown()
        //{
        //    Debug.Log($"--- OnBtnDown");

        //}

        //public void OnBtnUpOrExit()
        //{
        //    Debug.Log($"--- OnBtnUpOrExit");
        //}
    }
}
