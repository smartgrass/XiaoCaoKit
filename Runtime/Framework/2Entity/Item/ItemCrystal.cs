using cfg;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using System.CodeDom.Compiler;
using UnityEngine;
using XiaoCao;
public class ItemCrystal : ItemIdComponent
{
    public AudioClip hitClip;

    public Transform body;

    public int hp;

    public int maxHp = 3;

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
            OnDead();
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

    private void OnDead()
    {
        SoundMgr.Inst.PlayClip(deadClip);
        deadEvent?.Invoke();
        isDead = true;
        DoExploded();
        UIMgr.Inst.battleHud.RemoveItemHpBar(transform);
        if (transform.TryGetComponent<IExecute>(out IExecute execute))
        {
            execute.Execute();
        }
    }

    #region 爆炸动画

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    void DoExploded()
    {
        var mesh = body.GetComponent<MeshFilter>().mesh;
        Vector3 center = mesh.bounds.center;
        center = center + Vector3.down * mesh.bounds.size.y * 0.1f;

        //子物体移到外层,给子物体施力
        ExplodedPos(center);
        //隐藏原body
        HideSelf();
    }

    [Header("爆炸设定")]

    [SerializeField] private float _explosionRadius = 5;
    [SerializeField] private float _explosionForce = 500;
    private void ExplodedPos(Vector3 centerPos)
    {
        int len = body.childCount;
        for (int i = 0; i < len; i++)
        {
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

        XCTime.DelayRun(4.5f, HideAll).Forget();
    }

    private void HideAll()
    {
        gameObject.SetActive(false);
    }

    #endregion
}
