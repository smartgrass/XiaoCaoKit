using DG.Tweening;
using System.Collections.Generic;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;
using static XiaoCao.BattleHud;
using Image = UnityEngine.UI.Image;

namespace XiaoCao
{
    public class BattleHud : MonoBehaviour
    {
        public Dictionary<int, HpBar> barDic = new Dictionary<int, HpBar>();

        public Dictionary<Transform, HpBar> itemHpBarDic = new Dictionary<Transform, HpBar>();

        public GameObject worldHpBarPrefab;

        public Transform worldHpBarParent;

        public RectTransform aimImgRT;

        public ItemPopHud itemPopHud;

        public HpBar playerBar;

        public Transform bossBarParent;

        public TextMeshProUGUI lvText;

        public Canvas worldCanvas;

        public Button exitLevelBtn;

        public GameObject gameWinTip;

        // public RectTransform tipRect;
        private RectTransform _canRect;
        // private TMP_Text _tipText;
        public RectTransform popTipRect;
        private TMP_Text _popTipText;
        private CanvasGroup _popTipCanvasGroup;
        private Sequence _popTipTween;
        private Transform _popTipTarget;
        private float _popTipOffsetY;

        private AssetPool pool;

        private readonly Vector3 hidePos = new Vector3(0, -999, 0);

        private HashSet<int> dataChange = new HashSet<int>();

        private readonly Stack<HpBar> bossBarPool = new Stack<HpBar>();

        private HpBar bossBarTemplate;


        public void Init()
        {
            pool = PoolMgr.Inst.GetOrCreatPrefabPool(worldHpBarPrefab);
            InitBossBar();
            GameEvent.AddEventListener<int, RoleChangeType>(EGameEvent.RoleChange.ToInt(), OnEntityChange);
            GameEvent.AddEventListener<ENowAttr, float>(EGameEvent.LocalPlayerChangeNowAttr.ToInt(),
                LocalPlayerChangeNowAttr);
            GameEvent.AddEventListener<ELevelResult>(EGameEvent.LevelEnd.ToInt(), OnLevelEnd);
            worldCanvas.worldCamera = Camera.main;
            InitDamageText();
            gameObject.SetActive(true);
            playerBar.SetBarColors(false);
            exitLevelBtn.onClick.AddListener(OnExitLevelBtn);
            exitLevelBtn.transform.gameObject.SetActive(false);
            gameWinTip.gameObject.SetActive(false);
            _canRect = transform.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            // _tipText = tipRect.GetComponentInChildren<TMP_Text>(true);
            InitPopTip();
        }

        //ELevelResult
        private void OnLevelEnd(ELevelResult state)
        {
            exitLevelBtn.transform.gameObject.SetActive(true);
            if (state == ELevelResult.Success)
            {
                gameWinTip.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<int, RoleChangeType>(EGameEvent.RoleChange.ToInt(), OnEntityChange);
            GameEvent.RemoveEventListener<ENowAttr, float>(EGameEvent.LocalPlayerChangeNowAttr.ToInt(),
                LocalPlayerChangeNowAttr);
            GameEvent.RemoveEventListener<ELevelResult>(EGameEvent.LevelEnd.ToInt(), OnLevelEnd);
            _popTipTween?.Kill();
        }

        private void Update()
        {
            if (GameDataCommon.Current.gameState == GameState.Running)
            {
                NorHpBarUpdate();
                UpdateItemHpBar();
                TipUpdate();
                PopTipUpdate();
            }
        }

        #region HpBar

        //每帧都执行, 检查增加
        private void NorHpBarUpdate()
        {
            foreach (var item in RoleMgr.Inst.roleDic.Values)
            {
                Role role = item;
                if (null != role)
                {
                    if (role.IsPlayer)
                    {
                        UpdatePlayerHpBar(role);
                    }
                    else
                    {
                        UpdateEnemyHpBar(role);
                    }
                }
            }
        }

        private void UpdatePlayerHpBar(Role role)
        {
            if (role.IsRuning)
            {
                playerBar.UpdateHealthBar(role.Hp / (float)role.MaxHp);
                playerBar.UpdateArmorBar(role.ShowArmorPercentage);
                if (!dataChange.Contains((int)ENowAttr.Level))
                {
                    playerBar.UpdateLvText(GameDataCommon.LocalPlayer.Level);
                    dataChange.Add((int)ENowAttr.Level);
                }
            }
        }

        private void UpdateEnemyHpBar(Role role)
        {
            if (!CanShowEnemyHpBar(role))
            {
                HideEnemyHpBar(role.id);
                return;
            }

            barDic.TryGetValue(role.id, out var bar);
            if (bar == null)
            {
                bar = CreateEnemyHpBar(role);
                barDic[role.id] = bar; 
            }

            bar.UpdateHealthBar(role.Hp / (float)role.MaxHp);
            if (role.Hp <= 0)
            {
                bar.gameObject.SetActive(false);
            }
            bar.UpdateArmorBar(role.ShowArmorPercentage);
            if (IsBossBar(bar))
            {
                RefreshBossBarParentState();
            }
            else if (bar.gameObject.activeSelf)
            {
                bar.UpdatePosition();
            }
        }

        public HpBar AddItemHpBar(Transform target, Vector3 offset)
        {
            GameObject newBarGo = pool.Get();
            var bar = newBarGo.GetComponent<HpBar>();
            itemHpBarDic[target] = bar;
            bar.transform.SetParent(worldHpBarParent, false);
            bar.SetFollow(target, offset);
            return bar;
        }

        public void RemoveItemHpBar(Transform key)
        {
            if (itemHpBarDic.ContainsKey(key))
            {
                var bar = itemHpBarDic[key];
                bar.gameObject.SetActive(false);
                pool.Release(bar.gameObject);
                itemHpBarDic.Remove(key);
            }
        }

        public void RemoveItemHpBar(HpBar value)
        {
            foreach (var item in itemHpBarDic)
            {
                if (item.Value == value)
                {
                    itemHpBarDic.Remove(item.Key);
                }
            }
        }

        void UpdateItemHpBar()
        {
            foreach (var item in itemHpBarDic)
            {
                item.Value.UpdatePosition();
            }
        }

        #endregion


        //只有变动时才执行, 隐藏多余
        void OnEntityChange(int id, RoleChangeType roleChangeType)
        {
            if (roleChangeType == RoleChangeType.Add)
            {
                return;
            }

            //玩家不移除显示
            if (id == GameDataCommon.LocalPlayer.id)
            {
                Debug.Log($"-- player bar rm");
                return;
            }

            if (barDic.ContainsKey(id))
            {
                var bar = barDic[id];
                ReleaseEnemyHpBar(bar);
                barDic.Remove(id);
            }
        }

        private void InitBossBar()
        {
            if (bossBarParent == null)
            {
                bossBarParent = transform.Find(nameof(bossBarParent));
            }

            if (bossBarParent == null)
            {
                return;
            }

            bossBarParent.gameObject.SetActive(false);
            bossBarTemplate = bossBarParent.GetComponentInChildren<HpBar>(true);
            if (bossBarTemplate != null)
            {
                bossBarTemplate.gameObject.SetActive(false);
                bossBarPool.Push(bossBarTemplate);
            }
        }

        private HpBar CreateEnemyHpBar(Role role)
        {
            if (role.HasTag(RoleTagCommon.Boss))
            {
                var bossBar = GetBossHpBar(role);
                if (bossBar != null)
                {
                    return bossBar;
                }
            }

            GameObject newBarGo = pool.Get();
            var bar = newBarGo.GetComponent<HpBar>();
            bar.InitColor(role);
            bar.transform.SetParent(worldHpBarParent, false);
            bar.SetFollowRole(role);
            bar.UpdateLvText(role.Level);
            return bar;
        }

        private HpBar GetBossHpBar(Role role)
        {
            if (bossBarTemplate == null)
            {
                bossBarTemplate = bossBarParent.GetComponentInChildren<HpBar>(true);
                if (bossBarTemplate != null && bossBarPool.Count == 0 && !bossBarTemplate.gameObject.activeSelf)
                {
                    bossBarPool.Push(bossBarTemplate);
                }
            }

            HpBar bar = null;
            if (bossBarPool.Count > 0)
            {
                bar = bossBarPool.Pop();
            }
            else if (bossBarTemplate != null)
            {
                bar = Instantiate(bossBarTemplate, bossBarParent);
            }

            if (bar == null)
            {
                return null;
            }

            bar.gameObject.SetActive(true);
            bar.InitColor(role);
            bar.UpdateLvText(role.Level);
            RefreshBossBarParentState();
            return bar;
        }

        private bool IsBossBar(HpBar bar)
        {
            return bar != null && bossBarParent != null && bar.transform.IsChildOf(bossBarParent);
        }

        private void ReleaseEnemyHpBar(HpBar bar)
        {
            if (bar == null)
            {
                return;
            }

            bar.gameObject.SetActive(false);
            if (IsBossBar(bar))
            {
                bossBarPool.Push(bar);
                RefreshBossBarParentState();
                return;
            }

            pool.Release(bar.gameObject);
        }

        private bool CanShowEnemyHpBar(Role role)
        {
            if (!role.IsRuning || role.HasTag(RoleTagCommon.NoHpBar))
            {
                return false;
            }

            if (role.IsEnemyIdentity && !role.IsAiOn)
            {
                return false;
            }

            return true;
        }

        private void HideEnemyHpBar(int roleId)
        {
            if (!barDic.TryGetValue(roleId, out var bar))
            {
                return;
            }

            ReleaseEnemyHpBar(bar);
            barDic.Remove(roleId);
        }

        private void RefreshBossBarParentState()
        {
            if (bossBarParent == null)
            {
                return;
            }

            bool hasActiveBossBar = false;
            foreach (Transform child in bossBarParent)
            {
                if (child.gameObject.activeSelf)
                {
                    hasActiveBossBar = true;
                    break;
                }
            }

            bossBarParent.gameObject.SetActive(hasActiveBossBar);
        }


        private void LocalPlayerChangeNowAttr(ENowAttr attr, float num)
        {
            if (attr == ENowAttr.Hp)
            {
                //显示回血效果
                //飘字->绿色
                if (num == 0)
                {
                    return;
                }

                ETextColor color = num > 0 ? ETextColor.Recover : ETextColor.Injured;
                string numStr;
                if (-1 < num && num < 1)
                {
                    numStr = num.ToString("0.#");
                }
                else
                {
                    numStr = ((int)num).ToString();
                }

                ShowDamageText(numStr, GameDataCommon.LocalPlayer.transform.position, color);
            }
        }

        public void OnExitLevelBtn()
        {
            GameMgr.Inst.ShowLevelEndDialog();
        }


        #region DamageText

        public DamageTextSetting DamageUITSetting;
        public int maxTextCount = 6;
        public TMP_Text firstText;
        private Transform textParent;
        private List<TMP_Text> _damageTexts = new List<TMP_Text>();
        private List<DamageTextTween> _damageTextTweens = new List<DamageTextTween>();
        private Vector2 changeVec2;
        private int nextText;

        public void InitDamageText()
        {
            textParent = firstText.transform.parent;
            firstText.color = Color.white.SetAlpha(0);
            _damageTexts.Add(firstText);
            for (int i = 1; i < maxTextCount; i++)
            {
                TMP_Text newText = Instantiate(firstText, textParent);
                _damageTexts.Add(newText);
            }

            foreach (var item in _damageTexts)
            {
                _damageTextTweens.Add(new DamageTextTween { text = item });
            }
        }

        public void ShowDamageText(string num, Vector3 mTarget, ETextColor eTextColor = ETextColor.Nor)
        {
            //获取屏幕坐标  
            Vector3 mScreen = Camera.main.WorldToScreenPoint(mTarget);
            if (mScreen.z > 0)
            {
                TMP_Text t = _damageTextTweens[nextText].text;

                Sequence tween = _damageTextTweens[nextText].tween;

                if (tween != null)
                {
                    tween.Kill();
                }

                _damageTextTweens[nextText].tween = DOTween.Sequence();
                tween = _damageTextTweens[nextText].tween;

                nextText++;
                if (nextText >= _damageTexts.Count)
                {
                    nextText = 0;
                }

                changeVec2 = Vector2.Scale(DamageUITSetting.randomVec2, Random.insideUnitCircle); //波动值
                changeVec2 += DamageUITSetting.offset;

                mScreen.x += changeVec2.x;
                mScreen.y += changeVec2.y;
                t.transform.localScale =
                    Random.Range(DamageUITSetting.randomScaleVec2.x, DamageUITSetting.randomScaleVec2.y) * Vector3.one;

                float randomY = DamageUITSetting.MoveY *
                                Random.Range(DamageUITSetting.randomScaleVec2.x, DamageUITSetting.randomScaleVec2.y);

                if (eTextColor == ETextColor.Recover)
                {
                    t.text = $"+{num}";
                    mScreen.x += DamageUITSetting.offset.x;
                    mScreen.y += DamageUITSetting.offset.y;
                }
                else
                {
                    t.text = num;
                }


                t.transform.position = mScreen + DamageUITSetting.starY * Vector3.one;
                //tween.SetEase(DamageUITSetting.ease);
                //大小
                tween.Join(DOTween
                    .To(x => t.fontSize = (int)x, DamageUITSetting.frontSizeStart, DamageUITSetting.frontSizeEnd,
                        DamageUITSetting.flyTime / 2).SetLoops(2, LoopType.Yoyo));

                //位置
                tween.Join(t.transform.DOMoveY(mScreen.y + DamageUITSetting.MoveY, DamageUITSetting.flyTime / 2));
                //颜色
                t.color = GetStartColor(eTextColor);


                var textTween = t.DOColor(GetEndColor(eTextColor), DamageUITSetting.flyTime / 2);

                tween.Join(textTween);

                tween.OnComplete(() => { t.color = Color.white.SetAlpha(0); });
            }
        }

        public Color GetStartColor(ETextColor eTextColor)
        {
            switch (eTextColor)
            {
                case ETextColor.Nor:
                    return DamageUITSetting.startColor;
                case ETextColor.Recover:
                    return DamageUITSetting.recoverColor;
                case ETextColor.Injured:
                    return DamageUITSetting.injuredColor;
            }


            return DamageUITSetting.startColor;
        }

        public Color GetEndColor(ETextColor eTextColor)
        {
            switch (eTextColor)
            {
                case ETextColor.Recover:
                    return DamageUITSetting.recoverColor.SetAlpha(0.5f);
                case ETextColor.Injured:
                    return DamageUITSetting.injuredColor.SetAlpha(0.5f);
            }

            return DamageUITSetting.endColor;
        }


        public void AnimTargetFixUpdate()
        {
            Player0 player0 = GameDataCommon.LocalPlayer;
            if (player0 != null)
            {
                Role role = player0.data_R.lastEnemy;
                if (role != null && !role.IsDie)
                {
                    ShowAnimTarget(role.transform.position);
                }
                else
                {
                    HideAnimTarget();
                }
            }
            else
            {
                HideAnimTarget();
            }
        }

        internal void ShowAnimTarget(Vector3 position)
        {
            //Vector2 pos = WorldScreenHelper.WorldToAnchorPos(position,UIMgr.Inst.midCanvas.transform as RectTransform);
            aimImgRT.transform.position = position;
        }

        public void HideAnimTarget()
        {
            aimImgRT.transform.position = hidePos;
        }


        public class DamageTextTween
        {
            public TMP_Text text;
            public Sequence tween;
        }


        /// <summary>
        /// 伤害数字
        /// </summary>
        [System.Serializable]
        public class DamageTextSetting
        {
            public Ease ease = Ease.InOutBack;
            public float frontSizeStart = 33;
            public float frontSizeEnd = 44;

            public float starY = 10;
            public float MoveY = 60;
            public float flyTime = 0.5f;
            public Color startColor = Color.white;
            public Color endColor = new Color(1, 1, 1, 0.7f);
            public Vector2 randomVec2 = new Vector2(25, 40);
            public Vector2 randomScaleVec2 = new Vector2(0.5f, 1);
            public Vector2 offset = new Vector2(60, 100);

            public Color recoverColor = Color.green;
            public Color injuredColor = Color.red;
        }

        public enum ETextColor
        {
            Nor,
            Recover, //绿
            Injured, //红
        }

        #endregion

        #region triggerItemTip

        private UITipData tipData;

        /// <summary>
        /// 显示跟随指定目标的交互提示。
        /// </summary>
        public void ShowTriggerItemTip(string key, Transform target, float offsetY)
        {
            if (target == null)
            {
                return;
            }

            // tipRect.gameObject.SetActive(true);
            // _tipText.text = key.ToLocalizeStr();
            tipData.target = target;
            tipData.offsetY = offsetY;
            tipData.isShowing = true;
            tipData.uiKey = key;
            // UpdateTipRectPosition(tipRect, target, offsetY);
            GameDataCommon.LocalPlayer.playerData.isNorAtkBlock = true;
        }

        public void TipUpdate()
        {
            if (tipData.isShowing)
            {
                if (GameDataCommon.LocalPlayer != null)
                {
                    var inputData = GameDataCommon.LocalPlayer.playerData.inputData;
                    if (Input.GetKeyDown(KeyCode.J) || inputData.inputs[InputKey.NorAck])
                    {
                        TriggerUI();
                    }
                }
            }
        }

        public void TriggerUI()
        {
            GameEvent.Send<string>(EGameEvent.HomeItemMsg.ToInt(), tipData.uiKey);
        }

        public void HideTriggerItemTip()
        {
            // tipRect.gameObject.SetActive(false);
            tipData.isShowing = false;
            tipData.target = null;
            if (GameDataCommon.LocalPlayer != null)
            {
                GameDataCommon.LocalPlayer.playerData.isNorAtkBlock = false;
            }
        }

        /// <summary>
        /// 按目标世界坐标和高度偏移刷新提示 UI 位置。
        /// </summary>
        private bool UpdateTipRectPosition(RectTransform rect, Transform target, float offsetY)
        {
            if (rect == null || target == null)
            {
                return false;
            }

            if (_canRect == null)
            {
                Canvas canvas = transform.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    return false;
                }
            
                _canRect = canvas.GetComponent<RectTransform>();
            }

            Vector3 worldPos = target.position + Vector3.up * offsetY;
            rect.anchoredPosition = WorldScreenHelper.WorldToAnchorPos(worldPos, _canRect);
            return true;
        }

        #endregion

        #region PopTip

        /// <summary>
        /// 初始化弹出提示的文本和淡入淡出控制。
        /// </summary>
        private void InitPopTip()
        {
            if (popTipRect == null)
            {
                return;
            }

            _popTipText = popTipRect.GetComponentInChildren<TMP_Text>(true);
            _popTipCanvasGroup = popTipRect.GetComponent<CanvasGroup>();
            if (_popTipCanvasGroup == null)
            {
                _popTipCanvasGroup = popTipRect.gameObject.AddComponent<CanvasGroup>();
            }

            _popTipCanvasGroup.alpha = 0;
            popTipRect.gameObject.SetActive(false);
        }

        /// <summary>
        /// 在指定目标上方弹出一段本地化提示文本。
        /// </summary>
        public void PopTip(string str, Transform target, float offsetY)
        {
            if (popTipRect == null || target == null)
            {
                return;
            }

            if (_popTipText == null || _popTipCanvasGroup == null)
            {
                InitPopTip();
            }

            if (_canRect == null)
            {
                _canRect = transform.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            }

            if (_popTipText != null)
            {
                _popTipText.text = str.ToLocalizeStr();
            }

            _popTipTarget = target;
            _popTipOffsetY = offsetY;
            UpdateTipRectPosition(popTipRect, _popTipTarget, _popTipOffsetY);
            popTipRect.gameObject.SetActive(true);
            _popTipCanvasGroup.alpha = 1;

            _popTipTween?.Kill();
            _popTipTween = DOTween.Sequence();
            _popTipTween.AppendInterval(1);
            _popTipTween.Append(DOTween.To(() => _popTipCanvasGroup.alpha, value => _popTipCanvasGroup.alpha = value,
                0, 0.35f));
            _popTipTween.OnComplete(() =>
            {
                _popTipTarget = null;
                popTipRect.gameObject.SetActive(false);
            });
        }

        /// <summary>
        /// 刷新弹出提示的跟随位置。
        /// </summary>
        private void PopTipUpdate()
        {
            if (popTipRect == null || !popTipRect.gameObject.activeSelf)
            {
                return;
            }

            if (!UpdateTipRectPosition(popTipRect, _popTipTarget, _popTipOffsetY))
            {
                _popTipTween?.Kill();
                _popTipTarget = null;
                popTipRect.gameObject.SetActive(false);
            }
        }

        #endregion
        
    }

    public struct UITipData
    {
        public bool isShowing;
        public Transform target;
        public float offsetY;
        public string uiKey;
    }
}
