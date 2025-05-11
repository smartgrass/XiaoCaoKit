using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Animations;
using XiaoCaoEditor;
using XiaoCao;
using NaughtyAttributes;
using AssetEditor.Editor;

public class AddAnimParamWindow : XiaoCaoWindow
{
    public string parameterName = "NewFloatParameter"; // 默认参数名
    public float defaultFloat = 0f; // 默认值

    [MenuItem(XCEditorTools.AddAnimParamWindow)]
    public static void ShowWindow()
    {
        OpenWindow<AddAnimParamWindow>("XiaoCao调试面板");        
    }


    [Button("添加Parameter到选中")]
    private void AddParameter()
    {
        // 获取当前选中的对象
        Object[] selectedObjects = Selection.GetFiltered(typeof(AnimatorController), SelectionMode.DeepAssets);

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No Animator Controllers selected in the Project window.");
            return;
        }

        // 遍历所有选中的AnimatorController
        foreach (Object obj in selectedObjects)
        {
            AnimatorController controller = obj as AnimatorController;
            if (controller != null)
            {
                // 检查参数是否已存在
                if (!ControllerHasParameter(controller, parameterName))
                {
                    // 添加Float参数
                    AnimatorControllerParameter parameter = new AnimatorControllerParameter();
                    parameter.type = AnimatorControllerParameterType.Float;
                    parameter.name = parameterName;
                    parameter.defaultFloat = defaultFloat;
                    controller.AddParameter(parameter);
                    EditorUtility.SetDirty(controller);
                    Debug.Log($"Added Float parameter '{parameterName}' to {controller.name}", controller);
                }
                else
                {
                    Debug.LogWarning($"Parameter '{parameterName}' already exists in {controller.name}", controller);
                }
            }
        }

        // 标记需要保存
        AssetDatabase.SaveAssets();
    }

    private bool ControllerHasParameter(AnimatorController controller, string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == parameterName)
            {
                return true;
            }
        }
        return false;
    }
}