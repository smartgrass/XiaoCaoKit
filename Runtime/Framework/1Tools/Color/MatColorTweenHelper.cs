using DG.Tweening;
using UnityEngine;

public static class MatColorTweenHelper
{
    public static void DoColorTween(this Material mat, string fieldName, Color endColor, float duration)
    {
        int id = Shader.PropertyToID(fieldName.ToString());
        DoColorTween(mat, id, endColor,duration);
    }
    public static void DoColorTween(this Material mat, int id, Color endColor, float duration)
    {
        if (!mat.HasColor(id))
        {
            return;
        }
        Color startColor = mat.GetColor(id);
        Tween tween = DOTween.To((t) =>
        {
            mat.SetColor(id, Color.Lerp(startColor, endColor, t));
        }, 0, 1, duration);
    }
}
