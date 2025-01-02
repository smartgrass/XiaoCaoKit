using System;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    ///虚假的配置文件,主要是提供一个接口 GetRewardItem 获取奖励
    ///实际奖励可自定义在不同类中实现
    ///<see cref="RewardItemConfigSo"/>
    /// </summary>
    [Serializable]
    public abstract class BaseRewardItemConfigSo : ScriptableObject, IKey
    {
        public string key;
        public string Key => key;

        public abstract Item GetRewardItem(int level);
    }
}
