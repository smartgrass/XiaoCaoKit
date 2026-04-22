using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/BattleExtraItemConfigSo", fileName = "BattleExtraItemConfigSo")]
    public class BattleExtraItemConfigSo : ScriptableObject
    {
        public List<BattleExtraItemSubConfig> list = new List<BattleExtraItemSubConfig>();

        private Dictionary<string, BattleExtraItemSubConfig> _configMap;

        public BattleExtraItemSubConfig GetConfig(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            EnsureMap();
            if (_configMap.TryGetValue(id, out var config))
            {
                return config;
            }

            return null;
        }

        private void EnsureMap()
        {
            if (_configMap != null)
            {
                return;
            }

            _configMap = new Dictionary<string, BattleExtraItemSubConfig>();
            for (int i = 0; i < list.Count; i++)
            {
                var config = list[i];
                if (config == null || string.IsNullOrEmpty(config.id))
                {
                    continue;
                }

                _configMap[config.id] = config;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _configMap = null;
        }
#endif
    }

    [Serializable]
    public class BattleExtraItemSubConfig
    {
        public string id;
        public string desc;//描述
        public int count = 1;
        public bool isUnCount;
        public float cdTime = 5;
    }
}
