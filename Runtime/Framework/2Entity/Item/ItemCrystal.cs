using cfg;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class ItemCrystal : ItemIdComponent
{
    public AudioClip hitClip;

    public Transform body;

    public int hp;

    public int maxHp = 3;

    public bool isDead;

    public float shakeLen = 0.5f; 

    private Vector3 startBodyPos;

    private Tween tween;

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
        if (hp < 0)
        {
            deadEvent?.Invoke();
            SoundMgr.Inst.PlayClip(deadClip);
            TimeStopMgr.Inst.StopTimeSpeed(5);
            isDead = true;
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

 
        HitHelper.ShowDamageText(transform, 1, ackInfo);
        HitHelper.ShowHitEffect(transform, ackInfo);
    }


    [Button(enabledMode:EButtonEnableMode.Playmode)]
    void DoExploded()
    {
        var mesh = body.GetComponent<MeshFilter>().mesh;
        Vector3 center = mesh.bounds.center;
        center = center + Vector3.down * mesh.bounds.size.y *0.1f;
        

        //子物体移到外层,给子物体施力
        ExplodedPos(center);

        //隐藏原body
        body.gameObject.SetActive(false);
    }

    [Header("爆炸设定")]

    [SerializeField] private float _explosionRadius = 5;
    [SerializeField] private float _explosionForce = 500;
    private void ExplodedPos(Vector3 centerPos)
    {
        int len = body.childCount;
        Debug.Log($"--- len {len}");
        for (int i = 0; i < len; i++)
        {
            Debug.Log($"--- i {i}");
            Transform child = body.GetChild(0);
            child.SetParent(transform, true);
            child.gameObject.SetActive(true);
            var rb = child.GetComponent<Rigidbody>();
            if (rb == null) continue;
            rb.AddExplosionForce(_explosionForce, centerPos, _explosionRadius, 1);
        }

    }
}
