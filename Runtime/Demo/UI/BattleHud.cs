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

        public TextMeshProUGUI lvText;

        public Canvas worldCanvas;

        public Button exitLevelBtn;

        private AssetPool pool;

        private readonly Vector3 hidePos = new Vector3(0, -999, 0);

        private HashSet<int> dataChange = new HashSet<int>();


        public void Init()
        {
            pool = PoolMgr.Inst.GetOrCreatPrefabPool(worldHpBarPrefab);
            GameEvent.AddEventListener<int, RoleChangeType>(EGameEvent.RoleChange.ToInt(), OnEntityChange);
            GameEvent.AddEventListener<ENowAttr, float>(EGameEvent.LocalPlayerChangeNowAttr.ToInt(),
                LocalPlayerChangeNowAttr);
            GameEvent.AddEventListener<int>(EGameEvent.LevelEnd.ToInt(), OnLevelEnd);
            worldCanvas.worldCamera = Camera.main;
            InitDamageText();
            gameObject.SetActive(true);
            playerBar.SetBarColors(false);
            exitLevelBtn.onClick.AddListener(OnExitLevelBtn);
            exitLevelBtn.transform.gameObject.SetActive(false);
        }

        private void OnLevelEnd(int state)
        {
            exitLevelBtn.transform.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<int, RoleChangeType>(EGameEvent.RoleChange.ToInt(), OnEntityChange);
            GameEvent.RemoveEventListener<ENowAttr, float>(EGameEvent.LocalPlayerChangeNowAttr.ToInt(),
                LocalPlayerChangeNowAttr);
            GameEvent.RemoveEventListener<int>(EGameEvent.LevelEnd.ToInt(), OnLevelEnd);
        }

        private void Update()
        {
            if (GameDataCommon.Current.gameState == GameState.Running)
            {
                NorHpBarUpdate();
                UpdateItemHpBar();
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
                        UpdataPlayerHpBar(role);
                    }
                    else
                    {
                        UpdataEnemyHpBar(role);
                    }
                }
            }
        }

        private void UpdataPlayerHpBar(Role role)
        {
            if (role.IsRuning)
            {
                playerBar.UpdateHealthBar(role.Hp / (float)role.MaxHp);
                playerBar.UpdateArmorBar(role.ShowArmorPercentage);
                if (!dataChange.Contains((int)ENowAttr.Level))
                {
                    UpdateLvText(GameDataCommon.LocalPlayer.Level);
                    dataChange.Add((int)ENowAttr.Level);
                }
            }
        }

        private void UpdataEnemyHpBar(Role role)
        {
            if (role.IsRuning && !role.HasTag(RoleTagCommon.NoHpBar))
            {
                barDic.TryGetValue(role.id, out var bar);
                if (bar == null)
                {
                    GameObject newBarGo = pool.Get();
                    bar = newBarGo.GetComponent<HpBar>();
                    bar.InitColor(role);
                    barDic[role.id] = bar;
                    bar.transform.SetParent(worldHpBarParent, false);
                    bar.SetFollowRole(role);
                }

                bar.UpdateHealthBar(role.Hp / (float)role.MaxHp);
                bar.UpdateArmorBar(role.ShowArmorPercentage);
                bar.UpdatePostion();
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
                item.Value.UpdatePostion();
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

            if (barDic.ContainsKey(id))
            {
                var bar = barDic[id];
                bar.gameObject.SetActive(false);
                pool.Release(bar.gameObject);
                barDic.Remove(id);
            }
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

        void UpdateLvText(int lv)
        {
            lvText.text = $"Lv {lv}";
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
    }
}