using NaughtyAttributes;
using System;
using TEngine;
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
            GameEvent.AddEventListener<UIPanelType, bool>(EGameEvent.UIPanelBtnGlow.Int(), OnOpenEffect);
        }

        private void OnOpenEffect(UIPanelType type, bool isOpen)
        {
            if (type == this.type)
            {
                Transform redTf = FindRedDot();
                if (isOpen)
                {
                    if (redTf == null)
                    {
                        redTf = Instantiate(UIPrefabMgr.Inst.btnRedDot, transform).transform;
                        (redTf.transform as RectTransform).anchoredPosition = new Vector2(-5, -5);
                        redTf.name = "RedDot";
                    }
                }
                else
                {
                    if (redTf)
                    {
                        redTf.gameObject.SetActive(false);
                    }
                }
            }
        }

        public Transform FindRedDot()
        {
            return transform.Find("RedDot");
        }


        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<UIPanelType, bool>(EGameEvent.UIPanelBtnGlow.Int(), OnOpenEffect);
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

    }
}
