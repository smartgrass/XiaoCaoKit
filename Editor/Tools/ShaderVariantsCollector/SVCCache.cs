using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShaderVariantsCollector
{
    [Serializable]
    internal class SVCCacheData
    {
        public Dictionary<string, string[]> materialGuidToKeywords = new Dictionary<string, string[]>();
        public Dictionary<string, string> materialGuidToShaderGuid = new Dictionary<string, string>();
        public Dictionary<string, List<string>> materialGuidToPrefabGuids = new Dictionary<string, List<string>>();
        public Dictionary<string, long> guidToTimestamp = new Dictionary<string, long>();
        public long lastRebuildTicks;
    }

    internal static class SVCCache
    {
        private static readonly string CachePath = Path.Combine("Library", "ShaderVariantsCollectorCache.json");
        private static SVCCacheData data;
        private static bool loaded;

        private static void EnsureLoaded()
        {
            if (loaded) return;
            try
            {
                if (File.Exists(CachePath))
                {
                    var json = File.ReadAllText(CachePath);
                    data = JsonUtility.FromJson<SVCCacheData>(json);
                }
            }
            catch { }
            if (data == null) data = new SVCCacheData();
            loaded = true;
        }

        private static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(CachePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonUtility.ToJson(data, prettyPrint: false);
                File.WriteAllText(CachePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SVCCache save failed: {e.Message}");
            }
        }

        public static void Clear()
        {
            EnsureLoaded();
            data = new SVCCacheData();
            Save();
        }

        private static long GetAssetTimestamp(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;
            return File.GetLastWriteTimeUtc(path).Ticks;
        }

        private static bool IsUpToDate(string guid)
        {
            var ts = GetAssetTimestamp(guid);
            if (!data.guidToTimestamp.TryGetValue(guid, out var cached)) return false;
            return ts == cached;
        }

        private static void SetTimestamp(string guid)
        {
            data.guidToTimestamp[guid] = GetAssetTimestamp(guid);
        }

        // Build or update material index (keywords, shader guid)
        public static void EnsureMaterialIndex()
        {
            EnsureLoaded();
            var matGuids = AssetDatabase.FindAssets("t:Material");
            foreach (var guid in matGuids)
            {
                if (IsUpToDate(guid) && data.materialGuidToKeywords.ContainsKey(guid)) continue;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null) continue;
                var kws = (mat.shaderKeywords ?? new string[0]).Where(k => !string.IsNullOrEmpty(k)).Distinct().ToArray();
                string shaderGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mat.shader));
                data.materialGuidToKeywords[guid] = kws;
                data.materialGuidToShaderGuid[guid] = shaderGuid;
                SetTimestamp(guid);
            }
            Save();
        }

        // Build or update prefab index (material guid -> prefab guids)
        public static void EnsurePrefabIndex()
        {
            EnsureLoaded();
            // Simple heuristic: rebuild fully if cache older than assets changes
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var newMap = new Dictionary<string, List<string>>();

            foreach (var pguid in prefabGuids)
            {
                string ppath = AssetDatabase.GUIDToAssetPath(pguid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ppath);
                if (prefab == null) continue;
                try
                {
                    var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        var mats = r.sharedMaterials;
                        if (mats == null) continue;
                        foreach (var mat in mats)
                        {
                            if (mat == null) continue;
                            string mguid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mat));
                            if (string.IsNullOrEmpty(mguid)) continue;
                            if (!newMap.TryGetValue(mguid, out var list))
                            {
                                list = new List<string>();
                                newMap[mguid] = list;
                            }
                            list.Add(pguid);
                        }
                    }
                }
                catch { }
            }

            data.materialGuidToPrefabGuids = newMap;
            // update timestamps for prefabs
            foreach (var pguid in prefabGuids)
            {
                SetTimestamp(pguid);
            }
            Save();
        }

        public static void RefreshAll()
        {
            EnsureLoaded();
            data = new SVCCacheData();
            EnsureMaterialIndex();
            EnsurePrefabIndex();
        }

        public static bool HasIndexes()
        {
            EnsureLoaded();
            return data.materialGuidToKeywords.Count > 0 && data.materialGuidToPrefabGuids.Count > 0;
        }

        public static void EnsureIndexes()
        {
            EnsureMaterialIndex();
            EnsurePrefabIndex();
        }

        public static List<Material> GetMaterialsForVariant(ShaderVariantData variant)
        {
            EnsureLoaded();
            if (variant == null || variant.shader == null) return new List<Material>();

            EnsureMaterialIndex();

            string shaderGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(variant.shader));
            var targetKeywords = new HashSet<string>((variant.keywords ?? new List<string>()).Where(k => !string.IsNullOrEmpty(k)).Distinct());

            var resultGuids = data.materialGuidToShaderGuid
                .Where(kv => kv.Value == shaderGuid)
                .Select(kv => kv.Key);

            var materials = new List<Material>();
            foreach (var mguid in resultGuids)
            {
                if (!data.materialGuidToKeywords.TryGetValue(mguid, out var kws)) continue;
                var matKw = new HashSet<string>(kws);
                if (targetKeywords.IsSubsetOf(matKw))
                {
                    string path = AssetDatabase.GUIDToAssetPath(mguid);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat != null) materials.Add(mat);
                }
            }
            return materials;
        }

        public static List<UnityEngine.Object> GetPrefabsForMaterial(Material material)
        {
            EnsureLoaded();
            if (material == null) return new List<UnityEngine.Object>();
            EnsurePrefabIndex();
            string mguid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material));
            if (string.IsNullOrEmpty(mguid)) return new List<UnityEngine.Object>();
            if (!data.materialGuidToPrefabGuids.TryGetValue(mguid, out var pguids) || pguids == null) return new List<UnityEngine.Object>();
            var results = new List<UnityEngine.Object>();
            foreach (var pguid in pguids.Distinct())
            {
                string path = AssetDatabase.GUIDToAssetPath(pguid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) results.Add(prefab);
            }
            return results;
        }
    }
} 