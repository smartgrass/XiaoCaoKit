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

        public string readme = "exBuffInfos用于存放2级以上的信息,有cd时间的buff都应该配置在这";

        public List<ExBuffInfo> exBuffInfos = new List<ExBuffInfo>();

        private Dictionary<EBuff, BuffInfo> _buffDic;

        private void OnEnable()
        {
            Reload();
        }

        private void Reload()
        {
            _buffDic = new Dictionary<EBuff, BuffInfo>();
            foreach (var item in buffs)
            {
                _buffDic[item.eBuff] = item;
            }

            _exBuffDic = new Dictionary<EBuff, ExBuffInfo>();
            foreach (var exBuff in exBuffInfos)
            {
                _exBuffDic[exBuff.eBuff] = exBuff;
            }
        }

        public BuffInfo GetBuffInfo(EBuff eBuff)
        {
            if (_buffDic.ContainsKey(eBuff))
            {
                return _buffDic[eBuff];
            }
            else if (eBuff.GetBuffType() == EBuffType.Ex && GetExBuffInfo(eBuff, out var info))
            {
                return info.ToLevelBuffInfo(0);
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

        private bool GetExBuffInfo(EBuff eBuff, out ExBuffInfo info)
        {
            if (HasExInfo(eBuff))
            {
                info = _exBuffDic[eBuff];
                return true;
            }

            info = null;
            Debug.LogError($"--- no ex buff config {eBuff}");
            return false;
        }

        private Dictionary<EBuff, ExBuffInfo> _exBuffDic;

        public bool HasExInfo(EBuff buff)
        {
            return _exBuffDic.ContainsKey(buff);
        }
        

        public BuffInfo GetExBuffInfoWithLevel(EBuff eBuff, int level)
        {
            // BuffInfo level0Info = GetBuffInfo(eBuff);
            // if (level == 0)
            // {
            //     return level0Info;
            // }

            if (GetExBuffInfo(eBuff, out var exBuffInfo))
            {
                if (level > exBuffInfo.MaxLevel)
                {
                    Debug.LogError($"--- no level config for {eBuff} {level} max {exBuffInfo.MaxLevel}");
                    return exBuffInfo.ToLevelBuffInfo(exBuffInfo.MaxLevel);
                }

                return exBuffInfo.ToLevelBuffInfo(level);
            }

            Debug.LogError($"--- out of array {eBuff} {level - 1} {exBuffInfo.addInfoArray.Count} ");
            return GetBuffInfo(eBuff);
            ;
        }

        [Button]
        void CheckNoConfig()
        {
            OnEnable();
            bool change = false;
            foreach (EBuff item in Enum.GetValues(typeof(EBuff)))
            {
                if (item.GetBuffType() == EBuffType.Nor)
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
                else if (item.GetBuffType() == EBuffType.Ex)
                {
                    //检查exBuffInfos中是否有 item, 没有则添加
                    if (!_exBuffDic.ContainsKey(item))
                    {
                        exBuffInfos.Add(new ExBuffInfo()
                        {
                            eBuff = item,
                            addInfoArray = new List<FloatArray>()
                                { new FloatArray() { values = new float[1] { 0.1f } } }
                        });
                        change = true;
                        Debug.Log($"--- Add Ex -> {item}");
                    }
                }
            }

            if (!change)
            {
                Debug.Log($"--- All ready");
            }
            else
            {
                Reload();
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
            exBuffInfos.Sort((x, y) => x.eBuff.CompareTo(y.eBuff));

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

        public int GetMaxLevel(EBuff buff)
        {
            if (GetExBuffInfo(buff, out var info))
            {
                return info.MaxLevel;
            }

            return 0;
        }
    }

    [Serializable]
    public class ExBuffInfo
    {
        public EBuff eBuff;
        public int MaxLevel => addInfoArray.Count - 1;

        [SerializeField] public List<FloatArray> addInfoArray;

        public BuffInfo ToLevelBuffInfo(int level)
        {
            return new BuffInfo()
            {
                eBuff = eBuff,
                addInfo = addInfoArray[level].values
            };
        }
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