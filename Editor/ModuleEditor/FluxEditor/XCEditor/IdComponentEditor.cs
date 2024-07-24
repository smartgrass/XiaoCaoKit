using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;
using XiaoCao;

[CustomEditor(typeof(IdComponent), true)]
public class IdComponentEditor : Editor
{
    public IdComponent self;

    public Object DrawTarget;
    private void OnEnable()
    {
        self = (IdComponent)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (Application.isPlaying && GameDataCommon.Current.gameState == GameState.Running)
        {
            var entity = self.GetEntity();
            if (entity != null)
            {
                var role = entity as Role;
                ComponentViewHelper.Draw(role);
            }
        }
    }
}
