using DG.Tweening;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityEngine.UI;

namespace XiaoCao
{
    public class BattleHud : MonoBehaviour
    {
        public Dictionary<int, HpBar> barDic = new Dictionary<int, HpBar>();

        public GameObject worldHpBarPrefab;

        public Transform worldHpBarParent;

        public HpBar playerBar;

        public Canvas worldCanvas;

        private AssetPool pool;

        public void Init()
        {
            pool = new AssetPool(worldHpBarPrefab);
            GameEvent.AddEventListener<int, RoleChangeType>(EventType.RoleChange.Int(), OnEntityChange);
            worldCanvas.worldCamera = Camera.main;
            InitDamageText();
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<int, RoleChangeType>(EventType.RoleChange.Int(), OnEntityChange);
        }

        private void Update()
        {
            DebugGUI.Log("gameState", GameDataCommon.Current.gameState);
            if (GameDataCommon.Current.gameState == GameState.Running)
            {
                NorHpBarUpdate();
            }
        }
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
            }
        }
        private void UpdataEnemyHpBar(Role role)
        {
            if (!role.HasTag(RoleTagCommon.NoHpBar) && role.IsRuning)
            {
                //!role.IsDie && 
                barDic.TryGetValue(role.id, out var bar);
                if (bar == null)
                {
                    GameObject newBarGo = pool.Get();
                    bar = newBarGo.GetComponent<HpBar>();
                    bar.Init(role.RoleType);
                    barDic[role.id] = bar;
                    bar.transform.SetParent(worldHpBarParent, false);
                    bar.SetParent(role);
                }
                bar.UpdateHealthBar(role.Hp / (float)role.MaxHp);
                bar.UpdateArmorBar(role.ShowArmorPercentage);
                bar.UpdatePostion();

                if (role.IsDie)
                {
                    
                }

            }
        }



        //只有变动时才执行, 隐藏多余
        void OnEntityChange(int id, RoleChangeType roleChangeType)
        {
            if (roleChangeType == RoleChangeType.Add)
            {
                return;
            }

            foreach (var item in barDic)
            {
                if (!RoleMgr.Inst.roleDic.ContainsKey(item.Key))
                {
                    var bar = item.Value;
                    pool.Release(bar.gameObject);
                    barDic.Remove(item.Key);
                }
            }
        }

        private void HpBarUpdate()
        {

        }


        #region DamageText
        public DamageTextSetting DamageUITSetting;
        public int maxTextCount = 6;
        public Text firstText;
        private Transform textParent;
        private List<Text> _damageTexts = new List<Text>();
        private List<DamageTextTween> _damageTextTweens = new List<DamageTextTween>();
        private Vector2 changeVec2;
        private int nextText;

        public void InitDamageText()
        {
            textParent = firstText.transform;
            _damageTexts.Add(firstText);
            for (int i = 1; i < maxTextCount; i++)
            {
                Text newText = Instantiate(firstText, textParent);
                _damageTexts.Add(newText);
            }

            foreach (var item in _damageTexts)
            {
                _damageTextTweens.Add(new DamageTextTween { text = item });
            }
        }

        public void ShowDamageText(int num, Vector3 mTarget, bool isBlod = false)
        {
            //获取屏幕坐标  
            Vector3 mScreen = Camera.main.WorldToScreenPoint(mTarget);
            if (mScreen.z > 0)
            {
                Text t = _damageTextTweens[nextText].text;

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

                changeVec2 = Vector2.Scale(DamageUITSetting.randomVec2, Random.insideUnitCircle);  //波动值
                changeVec2 += DamageUITSetting.offset;

                mScreen.x += changeVec2.x;
                mScreen.y += changeVec2.y;
                t.transform.localScale = Random.Range(DamageUITSetting.randomScaleVec2.x, DamageUITSetting.randomScaleVec2.y) * Vector3.one;

                float randomY = DamageUITSetting.MoveY * Random.Range(DamageUITSetting.randomScaleVec2.x, DamageUITSetting.randomScaleVec2.y);

                t.text = (num).ToString();
                t.transform.position = mScreen;
                //tween.SetEase(DamageUITSetting.ease);
                //大小
                tween.Join(DOTween.To(x => t.fontSize = (int)x, DamageUITSetting.frontSizeStart, DamageUITSetting.frontSizeEnd, DamageUITSetting.flyTime / 2).SetLoops(2, LoopType.Yoyo));

                //位置
                tween.Join(t.transform.DOMoveY(mScreen.y + DamageUITSetting.MoveY, DamageUITSetting.flyTime / 2));
                //颜色
                t.color = DamageUITSetting.startColor;

                var textTween = t.DOColor(DamageUITSetting.endColor, DamageUITSetting.flyTime / 2);

                tween.Join(textTween);

                //tween.Append(DOTween.To(x => t.fontSize = (int)x, DamageUITSetting.frontSizeMid , DamageUITSetting.frontSizeStart, DamageUITSetting.flyTime/2));

                Color ac = Color.white;
                ac.a = 0;
                tween.OnComplete(() => { t.color = ac; });

                gameObject.SetActive(true);
            }
        }


        public class DamageTextTween
        {
            public Text text;
            public Sequence tween;
        }


        [System.Serializable]
        public class DamageTextSetting
        {
            public Ease ease = Ease.InOutBack;
            public float frontSizeStart = 33;
            public float frontSizeEnd = 44;

            public float MoveY = 60;
            public float flyTime = 0.5f;
            public Color startColor = Color.white;
            public Color endColor = new Color(1, 1, 1, 0.7f);
            public Vector2 randomVec2 = new Vector2(25, 40);
            public Vector2 randomScaleVec2 = new Vector2(0.5f, 1);
            public Vector2 offset = new Vector2(60, 100);
        }

        #endregion
    }
}


