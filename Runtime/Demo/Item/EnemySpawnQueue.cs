using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 关卡级敌人生成队列: 所有 EnemyCreator 的生成请求统一排队, 按 LevelActionChecker 配置的间隔逐个执行
    /// 把模型加载/实例化的峰值拆到多帧, 避免刚进关卡时所有敌人同一帧刷出来造成卡顿
    /// </summary>
    public static class EnemySpawnQueue
    {
        private struct Entry
        {
            public EnemyCreator creator;
            public Role target;
            //生成单位所属的敌人组, 用来判断是否切换了组(换组时额外等待 GroupInterval)
            public EnemyGroupComponent group;
            //作为本批次第一个生成单位时的额外等待, 对应 EnemyCreator.delayGen
            public float firstDelay;
            public bool useFirstDelay;
        }

        private static readonly Queue<Entry> _queue = new Queue<Entry>();

        //关卡切换时递增, 让还在等待中的旧队列自动退出
        private static int _version;
        private static bool _running;
        private static int _batchCount;
        //上一个已生成的单位所属的组, 用于识别换组
        private static EnemyGroupComponent _lastGroup;

        /// <summary>默认生成间隔(秒), 关卡没有配置 LevelActionChecker 时使用</summary>
        public const float DefaultInterval = 0.2f;

        /// <summary>
        /// 相邻两个生成单位的间隔(秒), 由 LevelActionChecker 在关卡初始化时注入
        /// 小于等于 0 表示不排队等待, 队列内的生成单位会在同一帧一次生成
        /// </summary>
        public static float Interval { get; private set; } = DefaultInterval;

        /// <summary>
        /// 换到另一个敌人组时的额外等待(秒), 叠加在 Interval 之上
        /// 让上一波敌人整体出完再开始下一波; 小于等于 0 表示组之间不额外等待
        /// </summary>
        public static float GroupInterval { get; private set; }

        /// <summary>
        /// 设置本关卡的生成间隔, 关卡初始化时由 LevelActionChecker 写入
        /// </summary>
        public static void Configure(float interval, float groupInterval = 0f)
        {
            Interval = Mathf.Max(0f, interval);
            GroupInterval = Mathf.Max(0f, groupInterval);
        }

        /// <summary>
        /// 丢弃尚未执行的生成请求(切关卡时调用), 已在等待中的队列会被版本号打断
        /// </summary>
        public static void Clear()
        {
            _queue.Clear();
            _version++;
            _running = false;
            _batchCount = 0;
            _lastGroup = null;
        }

        /// <summary>
        /// 入队一个生成单位, 队空时立即生效, 队非空时排在队尾按间隔依次生成
        /// </summary>
        /// <param name="group">所属敌人组, 换组时队列会额外等待 GroupInterval</param>
        /// <param name="useFirstDelay">作为本批次第一个生成单位时是否等待 firstDelay(进场预生成用)</param>
        public static void Enqueue(EnemyCreator creator, Role target, EnemyGroupComponent group = null,
            bool useFirstDelay = false, float firstDelay = 0f)
        {
            if (!creator)
            {
                return;
            }

            _queue.Enqueue(new Entry
            {
                creator = creator,
                target = target,
                group = group,
                firstDelay = firstDelay,
                useFirstDelay = useFirstDelay,
            });

            if (!_running)
            {
                _running = true;
                Pump(_version).Forget();
            }
        }

        /// <summary>
        /// 队列驱动: 逐个取出生成单位, 首个按 firstDelay 等待, 之后每个按 Interval 间隔
        /// </summary>
        private static async UniTaskVoid Pump(int version)
        {
            while (true)
            {
                if (_version != version)
                {
                    return;
                }

                if (_queue.Count == 0)
                {
                    _running = false;
                    _batchCount = 0;
                    _lastGroup = null;
                    return;
                }

                Entry entry = _queue.Dequeue();

                float wait;
                if (_batchCount == 0)
                {
                    //本批次第一个单位(走近触发/进场首个)不额外插组间隔, 保证触发即响应
                    wait = entry.useFirstDelay ? entry.firstDelay : 0f;
                }
                else
                {
                    wait = Interval;

                    //切换到了另一个敌人组: 在常规间隔之上再加一段, 让上一波敌人先整体登场
                    if (GroupInterval > 0f && entry.group && _lastGroup && entry.group != _lastGroup)
                    {
                        wait += GroupInterval;
                    }
                }

                if (wait > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(wait), DelayType.DeltaTime, PlayerLoopTiming.Update);
                }
                else
                {
                    //至少让出一帧, 保证同一帧内 LevelControl 已经把关卡配置写进 creator
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }

                if (_version != version)
                {
                    return;
                }

                //节点已被销毁(切关卡)时跳过, 不占用批次计数
                if (!entry.creator)
                {
                    continue;
                }

                entry.creator.TargetRole = entry.target;
                entry.creator.Execute();
                _batchCount++;

                //只记录有效的组, 不属于任何组的散落 creator 不参与换组判断
                if (entry.group)
                {
                    _lastGroup = entry.group;
                }
            }
        }
    }
}
