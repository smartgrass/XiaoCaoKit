using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;
using XiaoCao;

[CustomEditor(typeof(RoleDataViewer), true)]
public class RoleEditorViewerEditor : Editor
{
    public RoleDataViewer self;

    public Object DrawTarget;
    private void OnEnable()
    {
        self = (RoleDataViewer)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (Application.isPlaying && GameDataCommon.Current.gameState == GameState.Running)
        {
            var entity = self.entity;
            if (entity != null)
            {
                var role = entity ;
                ComponentViewHelper.Draw(entity);
            }
        }
    }
}
