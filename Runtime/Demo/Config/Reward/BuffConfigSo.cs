using NaughtyAttributes;
using OdinSerializer;
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
            foreach (EBuff item in Enum.GetValues(typeof(EBuff)))
            {
                if (!_buffDic.ContainsKey(item))
                {

                    buffs.Add(new BuffInfo()
                    {
                        eBuff = item,
                        addInfo = new float[1] { 1 }
                    });
                    Debug.Log($"--- add {item}");
                }
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

        [Button]
        void CheckSort()
        {
            List<BuffInfo> newBuffs  =new List<BuffInfo>(buffs);
            //?? TODO
            newBuffs.Sort((a, b) =>
            {
                if ((int)a.eBuff > (int)b.eBuff)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            });
            buffs = newBuffs;   

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

    }
}

