using System;
using UnityEngine;
using UnityEngine.UIElements;

public class HideMatTweenExec : DelayMonoExecute
{
    public float fadeTime = 1;
    public Color endColor;
    public EMatColorName fieldName;
    public override void ExecuteOnTime()
    {
        if (fieldName == EMatColorName.None)
        {
            return;
        }

        var renderers = transform.GetComponentsInChildren<MeshRenderer>();

        int id = Shader.PropertyToID(fieldName.ToString());

        foreach (var renderer in renderers)
        {
            var mat = renderer.material;
            float duration = 1;
            mat.DoColorTween(id, endColor, duration);
        }
    }

    public enum EMatColorName
    {
        None = 0,
        _EmissionColor
    }
}
