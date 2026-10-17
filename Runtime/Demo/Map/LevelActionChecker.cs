using NaughtyAttributes;
using OdinSerializer.Utilities;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
namespace XiaoCao
{
    /// <summary>
    /// 关卡起终点覆盖配置, 挂在LevelAction配置的同一节点上
    /// 关卡加载优先使用这里的起终点, 为空则走LevelControl原来的查找逻辑
    /// </summary>
    public class LevelActionChecker : GameStartMono
    {
        [Tooltip("关卡起始点覆盖; 为空则用LevelControl.startPoint, 再找子节点startPoint")]
        public Transform startPointOverrid;

        [Tooltip("关卡结束点覆盖; 为空则用LevelControl.endPoint, 再找子节点endPoint")]
        public Transform endPointOverrid;
    }
}