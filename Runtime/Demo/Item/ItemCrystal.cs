using cfg;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using Newtonsoft.Json.Linq;
using System.CodeDom.Compiler;
using System.Collections;
using UnityEditor;
using UnityEngine;
using XiaoCao;

public class ItemCrystal : ItemIdComponent
{
    public AudioClip hitClip;

    public Transform body;

    public Transform explodePoint;

    public int hp;

    public int maxHp = 3;

    public float fadeTime = 1.5f;
    public float preFadeTime = 1.5f;
    public FadeEffect fadeEffect;

    public bool isDead;

    public Vector3 hpBarOffset = Vector3.up;

    public float shakeLen = 0.5f;

    private Vector3 startBodyPos;

    private Tween tween;

    private HpBar hpBar;




    private void Start()
    {
        Init();
    }

    public void Init()
    {
        hp = maxHp;
        isDead = false;
        startBodyPos = body.localPosition;
    }

    public override void OnDamage(AtkInfo ackInfo)
    {
        if (isDead)
        {
            return;
        }

        hp--;
        HitHelper.ShowDamageText(transform, 1, ackInfo);
        HitHelper.ShowHitEffect(transform, ackInfo);

        SetHpBar();

        if (hp <= 0)
        {

            OnDead(ackInfo);
            return;
        }
        else
        {
            SoundMgr.Inst.PlayClip(hitClip);
        }
        if (tween != null)
        {
            tween.Kill();
        }

        tween = body.DOShakePosition(0.3f, shakeLen).OnComplete(() =>
        {
            body.localPosition = startBodyPos;
        });
    }

    private void SetHpBar()
    {
        if (hpBar == null)
        {
            hpBar = UIMgr.Inst.battleHud.AddItemHpBar(transform, hpBarOffset);
        }
        hpBar.UpdateHealthBar((float)hp / maxHp);
    }


    private void OnDead(AtkInfo ackInfo)
    {
        deadInfo = new DeadInfo()
        {
            killerId = ackInfo.atker,
        };
        SoundMgr.Inst.PlayClip(deadClip);
        deadEvent?.Invoke();
        isDead = true;
        DoExploded();
        //隐藏原body
        HideSelf();
        UIMgr.Inst.battleHud.RemoveItemHpBar(transform);

        ExecuteHelper.DoExecute(transform);
    }

    [Button("检查")]
    void Check()
    {
        Debug.Log($"--- set Layer and Tag");
        gameObject.layer = Layers.WALL;
        gameObject.tag = Tags.ITEM;

        Debug.Log($"--- 子物体col");
        bool find = false;
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform t = transform.GetChild(i);
            Collider col = t.GetComponent<Collider>();
            if (col != null)
            {
                find = true;
                col.isTrigger = true;
            }

            if (t.TryGetComponent<MeshRenderer>(out MeshRenderer mr) && t.childCount > 0)
            {
                foreach (Transform item in t)
                {
                    item.gameObject.SetActive(false);
                }
            }


        }

        if (!find)
        {
            Debug.LogError("--- need trigger in child");
        }

        if (transform.GetComponent<Rigidbody>() == null)
        {
            Debug.LogError($"--- need rig body");
        }

        if (transform.GetComponent<Collider>() == null)
        {
            Debug.LogError($"--- need col");
        }

        if (explodePoint == null)
        {
            explodePoint = new GameObject("explodePoint").transform;
            explodePoint.SetParent(transform);
        }

    }


    #region 爆炸动画

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    void DoExploded()
    {
        var mesh = body.GetComponent<MeshFilter>().mesh;
        Vector3 explodePos = explodePoint ? explodePoint.position : mesh.bounds.center;

        //子物体移到外层,给子物体施力
        ExplodedPos(explodePos);
    }

    [XCHeader("爆炸设定")]

    [SerializeField] private float _explosionRadius = 5;
    [SerializeField] private float _explosionForce = 500;
    private void ExplodedPos(Vector3 centerPos)
    {
        int len = body.childCount;
        for (int i = 0; i < len; i++)
        {
            //由于SetParent改变, 所以只需要拿到Child(0)
            Transform child = body.GetChild(0);
            child.SetParent(transform, true);
            child.gameObject.SetActive(true);
            var rb = child.GetComponent<Rigidbody>();
            if (rb == null) continue;
            rb.AddExplosionForce(_explosionForce, centerPos, _explosionRadius, 1);
        }
    }


    private void HideSelf()
    {
        if (transform.TryGetComponent<Collider>(out Collider col))
        {
            col.enabled = false;
        }
        body.gameObject.SetActive(false);

        if (fadeTime > 0 && fadeEffect != FadeEffect.None)
        {
            if (fadeEffect == FadeEffect.Alpha)
            {
                StartCoroutine(IEFadeAlphaAnim());
            }
            else
            {
                StartCoroutine(IEFadeDissolveAnim());
            }
        }
        else
        {
            XCTime.DelayRun(4f, HideAll).Forget();
        }
    }

    IEnumerator IEFadeDissolveAnim()
    {
        yield return new WaitForSeconds(preFadeTime);
        int len = transform.childCount;
        int id = Shader.PropertyToID("_Dissolve");
        for (int i = 0; i < len; i++)
        {
            Transform child = transform.GetChild(i);
            var renderer = child.GetComponent<MeshRenderer>();
            if (renderer == null) continue;
            if (!child.gameObject.activeInHierarchy) continue;
            renderer.material.DOFloat(1, id, fadeTime);
        }

        yield return new WaitForSeconds(fadeTime);
        HideAll();
    }


    IEnumerator IEFadeAlphaAnim()
    {
        yield return new WaitForSeconds(preFadeTime);
        int len = transform.childCount;
        int id = Shader.PropertyToID("_Alpha");
        for (int i = 0; i < len; i++)
        {
            Transform child = transform.GetChild(i);
            var renderer = child.GetComponent<MeshRenderer>();
            if (renderer == null) continue;
            if (!child.gameObject.activeInHierarchy) continue;
            renderer.material.DOFloat(0, id, fadeTime);
        }

        yield return new WaitForSeconds(fadeTime);
        HideAll();
    }

    private void HideAll()
    {
        gameObject.SetActive(false);
    }

    #endregion

    public enum FadeEffect
    {
        Alpha,
        Dissolve,
        None = -1
    }
}
