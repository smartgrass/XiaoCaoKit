using cfg;
using DG.Tweening;
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

    public override void OnDamage(int atker, AtkInfo ackInfo)
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
}
