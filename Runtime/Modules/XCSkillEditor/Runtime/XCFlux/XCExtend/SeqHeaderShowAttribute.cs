#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class SeqHeaderShowAttribute : PropertyAttribute
{
    public int line;
    public int posX;
    public int width;
    public string label;
    public string desLabel;

    public SeqHeaderShowAttribute(int line, int posX, int width,string label = null, string desLabel = null)
    {
        this.line = line;
        this.posX = posX;
        this.width = width;
        this.label = label;
        this.desLabel = desLabel;
    }

#if UNITY_EDITOR
    public GUIContent GetShowContent(SerializedProperty pro)
    {
        if (!string.IsNullOrEmpty(label))
        {
            return new GUIContent(label,desLabel);
        }
        return new GUIContent(pro.displayName);
    }
#endif
}


