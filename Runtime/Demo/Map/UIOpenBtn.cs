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

        //面板入口红点(如角色面板的技能学习红点), 只给需要提示的面板按钮配置
        public GameObject BtnRedDot;

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
            //红点相关: 技能红点变化 / 角色升级 / 持币变化都可能让"可学技能"状态改变
            GameEvent.AddEventListener(EGameEvent.SkillRedDotChange.ToInt(), RefreshRedDot);
            GameEvent.AddEventListener<int, int>(EGameEvent.RoleLevelUp.ToInt(), OnRoleLevelUp);
            GameEvent.AddEventListener<int>(EGameEvent.OnCoinChange.ToInt(), OnCoinChange);
            RefreshRedDot();
        }

        /// <summary>
        /// 刷新面板入口红点: 判断规则由各面板自己决定, 目前只有角色面板有技能学习红点
        /// </summary>
        public void RefreshRedDot()
        {
            //只有配了红点节点的按钮才需要刷新
            if (BtnRedDot == null)
            {
                return;
            }

            BtnRedDot.SetActive(type == UIPanelType.PlayerPanel && SkillLearnRedDot.HasAnyNew());
        }

        private void OnRoleLevelUp(int levelFrom, int levelTo)
        {
            RefreshRedDot();
        }

        private void OnCoinChange(int delta)
        {
            RefreshRedDot();
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
        /// 按钮发光特效(新手引导用): 与按钮等比平铺; 父节点无布局组时排在按钮之前(绘制在按钮底色之下), 有布局组时挂在按钮自身
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
        /// 创建发光节点: 与按钮完全重合。
        /// 父节点带布局组(BattleHud/TopBar/layout)时改挂到按钮自身, 否则发光会被布局当成一个元素把其它按钮挤开。
        /// </summary>
        private void CreateGlow(GameObject prefab)
        {
            bool onSelf;
            Transform parent = GuideGlowHelper.ResolveGlowParent(transform as RectTransform, out onSelf);
            _glowEffect = Instantiate(prefab, parent);
            _glowEffect.name = "BtnGlowEffect";

            ApplyButtonRect();

            if (onSelf)
            {
                //挂在自己身上时排最前: 绘制在自身底图之上、图标之下
                _glowEffect.transform.SetAsFirstSibling();
            }
            else
            {
                //插到按钮前面: sibling 越小绘制越靠后
                _glowEffect.transform.SetSiblingIndex(transform.GetSiblingIndex());
            }

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
            GuideGlowHelper.ApplyRect(transform as RectTransform, _glowEffect);
        }


        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<UIPanelType, bool>(EGameEvent.UIPanelBtnGlow.ToInt(), OnOpenEffect);
            GameEvent.RemoveEventListener(EGameEvent.SkillRedDotChange.ToInt(), RefreshRedDot);
            GameEvent.RemoveEventListener<int, int>(EGameEvent.RoleLevelUp.ToInt(), OnRoleLevelUp);
            GameEvent.RemoveEventListener<int>(EGameEvent.OnCoinChange.ToInt(), OnCoinChange);

            //发光可能挂在按钮自身或父节点下, 按钮销毁时手动清理
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
