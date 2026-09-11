// Editor/ArrangeChildren.cs 放在Editor文件夹
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ArrangeChildren : EditorWindow
{
    private float spacingX = 2f;
    private float spacingZ = 2f;
    private int perRowCount = 3;    // 每行几个，自动换行
    private bool centerPivot = true;// 是否整体居中

    [MenuItem("Tools/排列子物体(XZ平面,不修改Y)")]
    static void ShowWindow()
    {
        GetWindow<ArrangeChildren>("子物体XZ网格排列");
    }

    void OnGUI()
    {
        spacingX = EditorGUILayout.FloatField("X间距", spacingX);
        spacingZ = EditorGUILayout.FloatField("Z间距", spacingZ);
        perRowCount = EditorGUILayout.IntField("每行数量(换行)", perRowCount);
        centerPivot = EditorGUILayout.Toggle("整体居中", centerPivot);

        if (perRowCount < 1) perRowCount = 1;

        if (GUILayout.Button("执行排列"))
        {
            Transform parent = Selection.activeTransform;
            if (parent == null)
            {
                EditorUtility.DisplayDialog("提示", "请先选中父物体！", "OK");
                return;
            }

            Undo.RecordObject(parent, "XZ网格排列子物体");
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                children.Add(parent.GetChild(i));
            }

            int total = children.Count;
            int rowCount = Mathf.CeilToInt((float)total / perRowCount);

            Vector2 startOffset = Vector2.zero;
            if (centerPivot)
            {
                float totalWidth = (perRowCount - 1) * spacingX;
                float totalDepth = (rowCount - 1) * spacingZ;
                startOffset.x = -totalWidth / 2f;
                startOffset.y = -totalDepth / 2f;
            }

            for (int i = 0; i < total; i++)
            {
                int col = i % perRowCount;
                int row = i / perRowCount;

                float x = startOffset.x + col * spacingX;
                float z = startOffset.y + row * spacingZ;

                // ✅ 保留原有localY，只修改X,Z
                Vector3 oldPos = children[i].localPosition;
                children[i].localPosition = new Vector3(x, oldPos.y, z);
            }

            EditorUtility.SetDirty(parent);
        }
    }
}
