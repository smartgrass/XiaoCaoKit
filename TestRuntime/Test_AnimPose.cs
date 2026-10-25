#if UNITY_EDITOR
using System;
using System.IO;
using UniHumanoid;
using UnityEditor;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 模型使用的 VRM 版本。
    /// </summary>
    public enum EVrmVersion
    {
        /// <summary>没找到 VRM 组件, 判断不出版本</summary>
        Unknown,

        /// <summary>VRM0, 模型上带 VRMBlendShapeProxy / VRMMeta 等 VRM0 组件</summary>
        Vrm0,

        /// <summary>VRM1, 模型文件是 .vrm 结尾, 模型上带 Vrm10Instance</summary>
        Vrm1,
    }

    public class Test_AnimPose : MonoBehaviour
    {
        public string des;
        public float time;

        [NaughtyAttributes.Dropdown(nameof(GetModNameOptions))]
        public string modName = "Mod_001";

        public GameObject model;
        public AnimationClip clip;

        [NaughtyAttributes.ReadOnly]
        [Tooltip("最近一次判断出的模型版本: VRM1 的模型文件是 .vrm 结尾")]
        public EVrmVersion modelVersion;

        public const int Line0 = 0;
        public const int Line1 = 1;
        public const int Line2 = 2;

        public void ResetPlayer()
        {
            if (model != null && clip != null)
            {
                clip.SampleAnimation(model, 0);
                ////model.transform.localPosition = Vector3.zero;
            }
        }

        [NaughtyAttributes.Button("一键执行0~4", Line0)]
        void DoAll()
        {
            SetValue();
            RemoveComponents();
            AutoReplaceShadowMat();
            SetWeaponPoint();
            SavaPrefabToModeDir();
        }


        [NaughtyAttributes.Button("0.设置姿势", Line1)]
        public void SetValue()
        {
            if (model != null && clip != null)
            {
                model.GetComponent<Animator>().applyRootMotion = false;
                clip.SampleAnimation(model, time * clip.length);
            }
        }

        [NaughtyAttributes.Button("1.移除多余组件", Line1)]
        void RemoveComponents()
        {
            Component[] components = model.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component.GetType().Name.StartsWith("V"))
                {
                    string gameObjectName = component.gameObject.name;
                    GameObject.DestroyImmediate(component);
                    Debug.Log($"Destroyed component: {component.GetType().Name} {gameObjectName}");
                }
            }
        }


        [NaughtyAttributes.Button("2.替换阴影材质", Line1)]
        void AutoReplaceShadowMat()
        {
            // if (model.GetComponent<Humanoid>())
            // {
            //     return;
            // }
            if (modelVersion == EVrmVersion.Vrm1)
            {
                return;
            }


            Transform body = model.transform.Find("Body");
            if (body == null)
            {
                body = model.transform;
                Debug.LogError("没有找到body节点,改为全量替换材质");
            }
            UpdataMat(body);
            Transform hair = model.transform.Find("Hair");
            if (hair)
            {
                UpdataMat(hair);
            }


        }

        private static void UpdataMat(Transform body)
        {
            foreach (var renderer in body.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                MatExtend.FixMatToUniUnlitPro(renderer.sharedMaterials);
            }
        }

        [NaughtyAttributes.Button("3.添加武器节点", Line2)]
        void SetWeaponPoint()
        {
            var hand = model.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);

            //已存在则不重复添加
            Transform exist = hand.Find("WeaponPoint");
            if (exist != null)
            {
                Debug.Log($"--- 手部已有武器节点 [{exist.name}], 跳过添加");
                return;
            }

            var weaponPoint = transform.Find("WeaponPoint");
            GameObject wp = Instantiate(weaponPoint.gameObject, hand);
            wp.gameObject.name = "WeaponPoint";
            //wp.transform.SetParent(hand, true);
            wp.gameObject.SetActive(true);
            wp.transform.localPosition = Vector3.zero;
            wp.transform.localEulerAngles = Vector3.zero;
        }


        [NaughtyAttributes.Button("4.保存prefab到Mod文件夹", Line2)]
        void SavaPrefabToModeDir()
        {
            //将model保存到modName目录下,作为Prefab,名字保持不变
            //复制时,如果已存在则先删除旧的prefab
            if (model == null)
            {
                Debug.LogError("--- model 为空, 无法保存 prefab");
                return;
            }

            if (model.name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Debug.LogError($"--- model 名字包含非法字符, 无法作为 prefab 名: {model.name}");
                return;
            }

            string modDir = GetModDirPath();
            if (string.IsNullOrEmpty(modDir))
            {
                return;
            }

            string prefabPath = $"{modDir}/{model.name}.prefab";
            bool isSceneObject = model.scene.IsValid();
            string selfPath = isSceneObject
                ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model)
                : AssetDatabase.GetAssetPath(model);
            bool isSelfPrefab = !string.IsNullOrEmpty(selfPath)
                                && selfPath.Equals(prefabPath, StringComparison.OrdinalIgnoreCase);

            if (isSelfPrefab)
            {
                if (!isSceneObject)
                {
                    Debug.Log($"--- model 已经是目标 prefab {prefabPath}, 无需保存");
                    Selection.activeObject = model;
                    EditorGUIUtility.PingObject(model);
                    return;
                }

                //model 就是这个 prefab 的场景实例, 删除 prefab 会把场景对象一起销毁, 所以直接覆盖保存
                Debug.Log($"--- {prefabPath} 已存在且为当前 model 的来源 prefab, 直接覆盖保存");
            }
            else if (!DeleteOldPrefab(prefabPath))
            {
                return;
            }

            GameObject savedPrefab;
            if (isSceneObject)
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(model, prefabPath, InteractionMode.UserAction);
            }
            else
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(model, prefabPath, out bool saveSuccess);
                if (!saveSuccess)
                {
                    savedPrefab = null;
                }
            }

            if (savedPrefab == null)
            {
                Debug.LogError($"--- 保存 prefab 失败 {prefabPath}");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"--- 保存 prefab 成功 {prefabPath}");
            Selection.activeObject = savedPrefab;
            EditorGUIUtility.PingObject(savedPrefab);
        }


        //modName 支持 "Mod_001" 或 "Assets/Mod_001" 的写法; 找不到时按名字匹配已有目录, 仍然没有则新建
        private string GetModDirPath()
        {
            string name = modName == null ? string.Empty : modName.Trim().Replace('\\', '/').TrimEnd('/');
            if (name.Length == 0)
            {
                Debug.LogError("--- modName 为空, 无法保存 prefab");
                return null;
            }

            string modDir = name.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ? name : $"Assets/{name}";
            if (AssetDatabase.IsValidFolder(modDir))
            {
                return modDir;
            }

            //容错: Mod001 / mod_001 等写法也能匹配到已有目录
            string existDir = FindExistModDir(name);
            if (existDir != null)
            {
                Debug.Log($"--- modName {modName} 匹配到已有目录 {existDir}");
                return existDir;
            }

            string parentDir = Path.GetDirectoryName(modDir).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parentDir))
            {
                Debug.LogError($"--- 找不到 mod 目录 {modDir}, 父目录 {parentDir} 不存在");
                return null;
            }

            AssetDatabase.CreateFolder(parentDir, Path.GetFileName(modDir));
            AssetDatabase.Refresh();
            if (!AssetDatabase.IsValidFolder(modDir))
            {
                Debug.LogError($"--- 创建 mod 目录失败 {modDir}");
                return null;
            }

            Debug.Log($"--- 创建 mod 目录 {modDir}");
            return modDir;
        }

        private static bool DeleteOldPrefab(string prefabPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(prefabPath) == null)
            {
                return true;
            }

            Debug.Log($"--- 删除旧 prefab {prefabPath}");
            if (AssetDatabase.DeleteAsset(prefabPath))
            {
                return true;
            }

            Debug.LogError($"--- 删除旧 prefab 失败 {prefabPath}");
            return false;
        }

        //只匹配 Assets 下的第一层目录, 忽略下划线/横线和大小写差异
        private static string FindExistModDir(string modName)
        {
            string key = NormalizeModName(modName);
            foreach (string dir in Directory.GetDirectories(Application.dataPath))
            {
                string name = Path.GetFileName(dir);
                if (NormalizeModName(name) == key)
                {
                    return $"Assets/{name}";
                }
            }

            return null;
        }

        private static string NormalizeModName(string name)
        {
            return name.Replace("_", string.Empty).Replace("-", string.Empty).Trim().ToLowerInvariant();
        }

        //Inspector 下拉框, 直接选择 Assets 下已有的 Mod 目录
        private NaughtyAttributes.DropdownList<string> GetModNameOptions()
        {
            var list = new NaughtyAttributes.DropdownList<string>();
            string current = modName == null ? string.Empty : modName.Trim();
            bool hasCurrent = false;

            foreach (string dir in Directory.GetDirectories(Application.dataPath))
            {
                string name = Path.GetFileName(dir);
                if (!name.StartsWith("Mod", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(name, current, StringComparison.OrdinalIgnoreCase))
                {
                    hasCurrent = true;
                }

                list.Add(name, name);
            }

            if (current.Length > 0 && !hasCurrent)
            {
                list.Add($"{current} (没有该目录)", current);
            }

            return list;
        }

        [NaughtyAttributes.Button("选中武器节点", Line2)]
        void FindHand()
        {
            var tf = model.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
            Selection.activeObject = tf;
        }

        /// <summary>
        /// 判断模型是 VRM0 还是 VRM1: VRM1 的模型文件是 .vrm 结尾, 模型上会带 Vrm10Instance;
        /// VRM0 的模型则带 VRMBlendShapeProxy / VRMMeta。结果写进 modelVersion 并打印提示。
        /// </summary>
        [NaughtyAttributes.Button("判断模型版本(VRM0/VRM1)", Line1)]
        public EVrmVersion CheckModelVersion()
        {
            if (model == null)
            {
                modelVersion = EVrmVersion.Unknown;
                Debug.LogError("--- model 为空, 无法判断模型版本");
                return modelVersion;
            }
            if (model.TryGetComponent<UniHumanoid.Humanoid>(out Humanoid humanoid))
            {
                modelVersion = EVrmVersion.Vrm1;
                return modelVersion;
            }

            //VRM1 的模型文件是 .vrm 结尾, 导入后模型上一定带 Vrm10Instance
            if (model.GetComponentInChildren<UniVRM10.Vrm10Instance>(true) != null)
            {
                modelVersion = EVrmVersion.Vrm1;
                Debug.Log($"--- 模型 [{model.name}] 是 VRM1, 模型文件是 .vrm 结尾");
            }
            else if (model.GetComponentInChildren<VRM.VRMBlendShapeProxy>(true) != null
                     || model.GetComponentInChildren<VRM.VRMMeta>(true) != null)
            {
                modelVersion = EVrmVersion.Vrm0;
                Debug.Log($"--- 模型 [{model.name}] 是 VRM0");
            }
            else
            {
                modelVersion = EVrmVersion.Unknown;
                Debug.LogWarning($"--- 模型 [{model.name}] 上没找到 VRM 组件, 判断不出版本; VRM1 的模型文件是 .vrm 结尾");
            }

            return modelVersion;
        }
    }


    public static class ModelExtend
    {
        [MenuItem("CONTEXT/Transform/Vrm Remove Components")]
        private static void RemoveComponentsStartingWithV(MenuCommand command)
        {
            GameObject contextObject = ((Transform)command.context).gameObject;

            Component[] components = contextObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component.GetType().Name.StartsWith("V"))
                {
                    Debug.Log($"Destroyed component: {component.GetType().Name} from {contextObject.name}");
                    GameObject.DestroyImmediate(component);
                }
            }
        }
    }


    public class MatExtend
    {
        [MenuItem("Assets/Check/Change Shader to SimpleURPToonLitOutlineExample")]
        private static void ChangeShaderToSimpleURPToonLitOutlineExample(MenuCommand command)

        {

            var materials = new Material[Selection.objects.Length];

            for (int i = 0; i < Selection.objects.Length; i++)

            {

                var obj = Selection.objects[i];

                if (obj is Material material)

                {

                    materials[i] = material;

                }

                else if (obj is GameObject go)

                {

                    var renderer = go.GetComponent<Renderer>();

                    if (renderer != null && renderer.sharedMaterials.Length > 0)

                    {

                        // If multiple materials, clone them to avoid modifying shared materials

                        materials[i] = new Material(renderer.sharedMaterials[0]);

                    }

                }

            }



            foreach (var material in materials)

            {

                if (material != null)
                {
                    var shader = Shader.Find("SimpleURPToonLitExample(With Outline)");
                    if (shader != null)
                    {
                        if (material.HasProperty("_MainTex"))
                        {
                            var tex = material.GetTexture("_MainTex");
                            material.shader = shader;
                            material.SetTexture("_BaseMap", tex);
                        }
                        else
                        {
                            Debug.LogError($"--- no _MainTex skip {material.name} ");
                            continue;
                        }
                    }
                    else
                    {
                        Debug.LogError("Shader 'SimpleURPToonLitOutlineExample' not found!");
                    }

                    EditorUtility.SetDirty(material);
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
            }
        }


        [MenuItem("Assets/Check/UniUnlit->UniUnlitPro")]
        private static void ToUniLit(MenuCommand command)
        {
            var materials = new Material[Selection.objects.Length];
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                var obj = Selection.objects[i];
                if (obj is Material material)
                {
                    materials[i] = material;
                }
            }

            FixMatToUniUnlitPro(materials);
        }


        public static void FixMatToUniUnlitPro(Material[] materials)
        {
            foreach (var material in materials)
            {
                if (material != null)
                {
                    string shaderName = "UniGLTF/UniUnlitPro";
                    var shader = Shader.Find("UniGLTF/UniUnlitPro");
                    if (shader != null)
                    {
                        string originalRenderType = material.GetTag("RenderType", false, "Opaque");
                        Debug.Log($"--- RenderType {originalRenderType}");

                        material.SetOverrideTag(shaderName, originalRenderType);

                        string _CullMode = "_CullMode";
                        string _BlendMode = "_BlendMode";

                        float cullValue = material.GetFloat(_CullMode);
                        float blendValue = material.GetFloat(_BlendMode);

                        material.shader = shader;

                        Debug.Log($"--- _CullMode {material.GetFloat(_CullMode)} ->{cullValue}");
                        material.SetFloat(_CullMode, cullValue);

                        Debug.Log($"--- _BlendMode {material.GetFloat(_BlendMode)} ->{blendValue}");
                        material.SetFloat(_BlendMode, blendValue);
                    }
                    else
                    {
                        Debug.LogError($"Shader {shaderName} not found!");
                    }

                    EditorUtility.SetDirty(material);
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
            }
        }
    }

}

#endif