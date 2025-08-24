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

        public List<ExBuffInfo> exBuffInfos = new List<ExBuffInfo>();

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

        private ExBuffInfo GetExBuffInfo(EBuff eBuff)
        {
            foreach (var exBuff in exBuffInfos)
            {
                if (exBuff.eBuff == eBuff)
                {
                    return exBuff;
                }
            }
            Debug.LogError($"--- no ex buff config {eBuff}");
            // 返回一个默认的 ExBuffInfo，避免空引用
            return null;
        }


        public BuffInfo GetLevelBuffInfo(EBuff eBuff, int level)
        {
            BuffInfo level0Info = GetBuffInfo(eBuff);
            if (level == 0)
            {
                return level0Info;
            }

            ExBuffInfo exBuffInfo = GetExBuffInfo(eBuff);

            if (exBuffInfo == null)
            {
                return level0Info;
            }

            if (level > exBuffInfo.maxLevel)
            {
                Debug.LogError($"--- no level config for {eBuff} {level} max {exBuffInfo.maxLevel}");
                return level0Info;
            }

            if (level - 1 < exBuffInfo.addInfoArray.Count)
            {
                level0Info.addInfo = exBuffInfo.addInfoArray[level - 1].values;
            }
            else
            {
                Debug.LogError($"--- out of array {eBuff} {level - 1} {exBuffInfo.addInfoArray.Count} ");
            }

            return level0Info;
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

        public int GetMaxLevel(EBuff buff)
        {
            var exBuffInfo = GetExBuffInfo(buff);
            if (exBuffInfo != null)
            {
                return exBuffInfo.maxLevel;
            }
            return 0;
        }
    }

    [Serializable]
    public class ExBuffInfo
    {
        public EBuff eBuff;
        public int maxLevel = 1;
        [SerializeField]
        public List<FloatArray> addInfoArray;
    }

    [Serializable]
    public struct FloatArray
    {
        public float[] values;
        public float this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }
    }
}

