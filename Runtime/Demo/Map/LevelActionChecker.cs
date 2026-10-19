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

        [Header("敌人生成")]
        [Tooltip("关卡内敌人按顺序逐个生成的间隔(秒), 关卡初始化时自动下发给生成队列; 0 表示不排队, 一次性生成")]
        public float enemyGenInterval = 0.2f;

        [Tooltip("不同敌人组之间的额外等待(秒), 叠加在 enemyGenInterval 之上; 0 表示组之间不额外等待")]
        public float enemyGroupGenInterval = 0.5f;

        [Header("掉出地图保护")]
        [Tooltip("位置持续下降超过该时间(秒)判定为意外坐标, 回退到最近的安全点")]
        public float fallOutTime = 7f;

        [Tooltip("回退后仍持续下落的最大重试次数, 超过则按阵亡处理, 避免敌人卡在地图外导致关卡无法结算")]
        public int maxFallOutRecover = 2;
    }
}