using DG.Tweening.Plugins.Core.PathCore;
using NaughtyAttributes;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using XiaoCao;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace AssetEditor.Editor
{
    public class XCRepalceGuidWin : XiaoCaoWindow
    {
        public Object oldObject;
        public Object newObject;
        public string guid;
        [Label("保持命名")]
        public bool isReName;
        private string _oldName;

        [Button("替换并删除")]
        void ReplaceAnim()
        {
            if (oldObject != null)
            {
                string oldPath = AssetDatabase.GetAssetPath(oldObject);
                guid = AssetDatabase.AssetPathToGUID(oldPath);
                AssetDatabase.DeleteAsset(oldPath);
                _oldName = Path.GetFileNameWithoutExtension(oldPath);
            }
            SetGUID(newObject, guid);
        }

        public void SetGUID(Object asset, string _newGuid)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            string metaFile = assetPath + ".meta";
            string _oldGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));

            var content = File.ReadAllText(metaFile);
            if (Regex.IsMatch(content, _oldGuid))
            {
                content = content.Replace(_oldGuid, _newGuid);
                File.WriteAllText(metaFile, content);
                Debug.Log($"--- Replace");
                AssetDatabase.Refresh();
                if (isReName)
                {
                    AssetDatabase.RenameAsset(assetPath, _oldName);
                }
            }
        }

    }
}