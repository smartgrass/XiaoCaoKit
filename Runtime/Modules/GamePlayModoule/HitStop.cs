using UnityEngine;
using System.Collections;
using XiaoCao;
using DG.Tweening;
using UnityEngine.TextCore;
using DG.Tweening.Core;

public class HitStop : MonoSingleton<HitStop>
{
    //和顿帧时间的相关倍数
    public float shakeTimeFactor = 0.25f;
    public float shakeLength = 0.25f;
    public int shakePower = 10;

    public Coroutine currentDo;

    bool waiting = false;
    float refSmooth;
    float waitingTime; //等待时间
    float lastWaitlen;

    bool isEnbleHitShop = true;

    public static void Do(float time = 0.001f)
    {
        Inst.DoHitStop(time, time * Inst.shakeTimeFactor);
    }

    public void DoHitStop(float time = 0.001f, float ShakeTime = 0)
    {
        if (!isEnbleHitShop)
            return;

        if (time < 0)
        {
            return;
        }

        if (waiting)
        {
            if (lastWaitlen < time)
            {
                //时停50%累加
                waitingTime += 0.5f * time;
            }
        }
        else
        {
            lastWaitlen = time;
            StartCoroutine(Wait(time, ShakeTime));
        }

    }


    IEnumerator Wait(float time, float ShakeTime = 0)
    {
        waiting = true;
        waitingTime = Time.unscaledTime + time;

        while (waitingTime > Time.unscaledTime)
        {
            Time.timeScale = Mathf.SmoothDamp(Time.timeScale, 0.1f, ref refSmooth, 0.2f);
            yield return null;
        }
        if (ShakeTime > 0)
        {
            Shake(ShakeTime);
        }
        Time.timeScale = 1.0f;
        waiting = false;
    }

    private Tween shakeTween;

    public void Shake(float time)
    {
        if (null != shakeTween)
        {
            shakeTween.Pause();
        }

        shakeTween = Camera.main.DOShakePosition(time, shakeLength, shakePower);
    }

    public void Cancel()
    {
        if (currentDo != null)
            StopCoroutine(currentDo);
        Time.timeScale = 1.0f;
        waiting = false;
    }

}


public static class HitTween
{

    public static Tween DOHit(this CharacterController cc, float totalY, Vector3 horVec, float duration)
    {
        Transform tf = cc.transform;
        if (totalY == 0)
        {
            totalY = 0.1f;
        }

        float time = 0;
        float lastT = 0, deltaT = 0;

        //0 ->1的数值动画
        Tween tween = DOTween.To(x => time = x, 0, 1, duration);
        tween.SetEase(Ease.OutQuart);
        Vector3 targetMove = horVec;
        targetMove.y += totalY; //目标移动量

        tween.OnUpdate(() =>
        {
            deltaT = time - lastT;
            lastT = time;
            Vector3 delta = targetMove * deltaT;
            cc.Move(delta);
        });
        return tween;
    }

    public static Tween DoMoveTo(this CharacterController cc, Vector3 pos, float duration, Ease ease = Ease.InOutQuart)
    {
        float time = 0;
        float lastT = 0, deltaT = 0;

        Tween tween = DOTween.To(x => time = x, 0, 1, duration);
        

        tween.SetEase(ease);

        Vector3 endPos = pos;   
        Vector3 startPos = cc.transform.position;
        float distance = Vector3.Distance(startPos, endPos);
        
        tween.OnUpdate(() =>
        {
            deltaT = time - lastT;
            lastT = time;
            Vector3 dir =(endPos - cc.transform.position).normalized;
            Vector3 delta = dir * distance * deltaT;
            cc.Move(delta);
        });

        return tween;
    }

    //public static Tween DoShake(Transform tf = )
    //{
    //    Transform tf = 


    //    float time = 0;
    //    float lastT = 0, deltaT = 0;

    //    //0 ->1的数值动画
    //    Tween tween = DOTween.To(x => time = x, 0, 1, duration);
    //    tween.SetEase(Ease.OutQuart);

    //}
}