using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class MoveTweenExec : MonoExecute
{
    public float duration = 0.8f;

    public Vector3 moveVec = Vector3.down;

    public bool isRelative = true;

    public bool hideOnFinish = false;

    public override void Execute()
    {
        var tween = transform.DOMove(moveVec, duration);
        if (isRelative)
        {
            tween.SetRelative();
        }
        tween.onComplete += OnFinsh;
    }

    public void OnFinsh()
    {
        if (hideOnFinish)
        {
            gameObject.SetActive(false);
        }
    }

}
