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
    public class UIOpenBtn : MonoBehaviour
    {
        public bool IsHideSelf;

        public UIPanelType type;

        //新手引导用的按钮发光特效
        private GameObject _glowEffect;

        //发光动画组件(预制体不会自动播放, 需要手动 Play)
        private SimpleImageTween _glowTween;

        //发光呼吸的缩放倍数
        private const float GlowScaleMult = 1.2f;

        private void Start()
        {
            transform.GetComponent<Button>().onClick.AddListener(Open);
            GameEvent.AddEventListener<UIPanelType, bool>(EGameEvent.UIPanelBtnGlow.ToInt(), OnOpenEffect);
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
                        redTf = Instantiate(UIPrefabSo.Inst.btnRedDot, transform).transform;
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

        /// <summary>
        /// 按面板类型查找打开按钮
        /// </summary>
        public static UIOpenBtn Find(UIPanelType panelType)
        {
            UIOpenBtn[] buttons = FindObjectsByType<UIOpenBtn>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].type == panelType)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 按钮发光特效(新手引导用): 与按钮等比平铺, 排在按钮之前(绘制在按钮底色之下)
        /// </summary>
        public void SetGlow(bool isOn)
        {
            if (isOn)
            {
                UIPrefabSo so = UIPrefabSo.Inst != null ? UIPrefabSo.Inst : Resources.Load<UIPrefabSo>("UIPrefabSo");
                if (_glowEffect == null && so != null && so.btnGlowEffect != null)
                {
                    CreateGlow(so.btnGlowEffect);
                }

                if (_glowEffect != null)
                {
                    //每次开启都重新对齐, 防止按钮尺寸在布局之后才确定
                    ApplyButtonRect();
                    if (!_glowEffect.activeSelf)
                    {
                        _glowEffect.SetActive(true);
                    }

                    //动画不会自己开始, 激活后手动播放
                    PlayGlow();
                }
            }
            else if (_glowEffect != null && _glowEffect.activeSelf)
            {
                if (_glowTween != null)
                {
                    _glowTween.Stop();
                }
                _glowEffect.SetActive(false);
            }
        }

        /// <summary>
        /// 创建发光节点: 挂在按钮父节点下, 尺寸与按钮完全重合
        /// </summary>
        private void CreateGlow(GameObject prefab)
        {
            //放到父节点才能排在按钮之前, 否则会盖住按钮自身的底图
            Transform parent = transform.parent != null ? transform.parent : transform;
            _glowEffect = Instantiate(prefab, parent);
            _glowEffect.name = "BtnGlowEffect";

            ApplyButtonRect();

            //插到按钮前面: sibling 越小绘制越靠后
            _glowEffect.transform.SetSiblingIndex(transform.GetSiblingIndex());

            _glowTween = _glowEffect.GetComponent<SimpleImageTween>();
            if (_glowTween != null)
            {
                _glowTween.scaleMult = GlowScaleMult;
            }
        }

        /// <summary>
        /// 启动发光动画: 组件本身没有 Start 自动播放
        /// </summary>
        private void PlayGlow()
        {
            if (_glowTween != null && _glowEffect != null && _glowEffect.activeInHierarchy)
            {
                _glowTween.Play();
            }
        }

        /// <summary>
        /// 让发光与按钮完全重合
        /// </summary>
        private void ApplyButtonRect()
        {
            RectTransform rt = _glowEffect != null ? _glowEffect.transform as RectTransform : null;
            RectTransform selfRt = transform as RectTransform;
            RectTransform parentRt = _glowEffect != null ? _glowEffect.transform.parent as RectTransform : null;
            if (rt == null || selfRt == null || parentRt == null)
            {
                return;
            }

            //用世界角点换算到父节点局部坐标, 兼容按钮自身的缩放
            Vector3[] corners = new Vector3[4];
            selfRt.GetWorldCorners(corners);
            Vector2 min = parentRt.InverseTransformPoint(corners[0]);
            Vector2 max = parentRt.InverseTransformPoint(corners[2]);

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = max - min;
            rt.anchoredPosition = (min + max) * 0.5f - parentRt.rect.min;
        }


        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<UIPanelType, bool>(EGameEvent.UIPanelBtnGlow.ToInt(), OnOpenEffect);

            //发光挂在父节点下, 按钮销毁时手动清理
            if (_glowEffect != null)
            {
                Destroy(_glowEffect);
                _glowEffect = null;
                _glowTween = null;
            }
        }

        [Button]
        private void Open()
        {
            UIMgr.Inst.ShowView(type);
        }

    }
}
