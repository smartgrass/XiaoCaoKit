using UnityEngine;
using UnityEditor;
using System.IO;
using FluxEditor;
using NaughtyAttributes;
using XiaoCao;

// 假设 FSequenceSubEditor 已经定义，这里省略其具体实现
public class FCreateSettingSubEditor : FSequenceSubEditor
{
    [Label("输入命名")]
    public string str = "Lancer";
    public string raceId="99";

    [Button("创建Setting")]
    void Create()
    {
        // 1. 创建XCSeqSetting在特定位置
        string seqSettingPath = $"Assets/Editor/SeqSetting/Enemy_{raceId}_{str}.asset";
        XCSeqSetting seqSetting = ScriptableObject.CreateInstance<XCSeqSetting>();
        seqSetting.raceId = raceId;

        // 创建文件夹（如果不存在）
        string directory = Path.GetDirectoryName(seqSettingPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        AssetDatabase.CreateAsset(seqSetting, seqSettingPath);
        AssetDatabase.SaveAssets();

        // 2. 根据raceId复制一个Assets/_Res/Role/AnimController/BaseAnim.controller, 并重命名
        string sourceControllerPath = "Assets/_Res/Role/AnimController/BaseAnim.controller";
        string editorControllerPath = "Assets/XiaoCaoKit/Res/EditorRes/EditorPlayerAnim.controller";
        string targetControllerPath = $"Assets/_Res/Role/AnimController/{raceId}.controller";

        // 使用 AssetDatabase 复制文件
        if (AssetDatabase.CopyAsset(sourceControllerPath, targetControllerPath))
        {
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError($"复制文件 {sourceControllerPath} 到 {targetControllerPath} 失败。");
            return;
        }

        // 3. 新创建的XCSeqSetting的targetAnimtorController赋值为步骤2创建的controller, 并赋值raceId
        RuntimeAnimatorController targetController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(targetControllerPath);
        seqSetting.targetAnimtorController = targetController;
        seqSetting.forEditorAC = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(editorControllerPath);

        EditorUtility.SetDirty(seqSetting);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"成功创建XCSeqSetting: {seqSettingPath} 和 AnimatorController: {targetControllerPath}");
    }



    [Button("创建Body")]
    void CreateBodyPrefab()
    {
        // 1. 复制Assets/_Res/Role/Enemy/E_0.prefab,并重命名
        string sourcePrefabPath = "Assets/_Res/Role/Enemy/E_0.prefab";
        string targetPrefabName = $"E_{raceId}_{str}.prefab";
        string targetPrefabPath = $"Assets/_Res/Role/Enemy/{targetPrefabName}";

        if (AssetDatabase.CopyAsset(sourcePrefabPath, targetPrefabPath))
        {
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError($"复制文件 {sourcePrefabPath} 到 {targetPrefabPath} 失败。");
            return;
        }

        // 2. 加载步骤1复制出的资源,读取IdRole组件,设置raceId和bodyName
        GameObject prefabInstance = AssetDatabase.LoadAssetAtPath<GameObject>(targetPrefabPath);
        if (prefabInstance != null)
        {
            IdRole idRole = prefabInstance.GetComponent<IdRole>();
            if (idRole != null)
            {
                idRole.raceId = int.Parse(raceId);
                idRole.bodyName = $"Body_E_{raceId}_{str}";

                // 保存修改
                EditorUtility.SetDirty(prefabInstance);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"成功创建并设置Body预制体: {targetPrefabPath}");
            }
            else
            {
                Debug.LogError($"预制体 {targetPrefabPath} 上未找到IdRole组件。");
            }
        }
        else
        {
            Debug.LogError($"无法加载预制体: {targetPrefabPath}");
        }
    }

}