using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;



namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/BuffConfigSo", fileName = "BuffConfigSo")]
    public class BuffConfigSo : ScriptableObject
    {
        public List<BuffInfo> buffs = new List<BuffInfo>();

        private Dictionary<EBuff, BuffInfo> _buffDic;

        private void OnEnable()
        {
            _buffDic = new Dictionary<EBuff, BuffInfo>();
            foreach (var item in buffs)
            {
                _buffDic[item.eBuff] = item;
            }
        }

        public BuffInfo GetBuffInfo(EBuff eBuff)
        {
            if (_buffDic.ContainsKey(eBuff))
            {
                return _buffDic[eBuff];
            }
            else
            {
                Debug.LogError($"--- no buff config {eBuff}");
                return new BuffInfo()
                {
                    eBuff = eBuff,
                    addInfo = new float[1] { 5 }
                };
            }
        }


        [Button]
        void CheckNoConfig()
        {
            OnEnable();
            bool change = false;
            foreach (EBuff item in Enum.GetValues(typeof(EBuff)))
            {
                if (!_buffDic.ContainsKey(item))
                {
                    buffs.Add(new BuffInfo()
                    {
                        eBuff = item,
                        addInfo = new float[1] { 0.1f }
                    });
                    change = true;
                    Debug.Log($"--- Add -> {item}");
                }
            }
            if (!change)
            {
                Debug.Log($"--- All ready");
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

        [Button]
        void CheckSort()
        {
            buffs.Sort((x, y) => x.eBuff.CompareTo(y.eBuff));

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

    }
}

