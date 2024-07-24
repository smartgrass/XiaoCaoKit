using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Test_ObjectView), true)]
public class Test_ObjectViewEditor : Editor
{
    public Test_ObjectView self;

    public Object DrawTarget;
    private void OnEnable()
    {
        self = (Test_ObjectView)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ComponentViewHelper.Draw(self.obj);
    }
}