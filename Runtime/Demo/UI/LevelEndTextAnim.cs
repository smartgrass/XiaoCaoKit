using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace XiaoCaoKit
{
    /// <summary>
    /// 结算面板文本的入场表现: 子节点依次从左往右滑入, 每个节点错开固定间隔。
    /// 子节点的原始位置在首次收集时缓存, 重复播放不会越滑越偏。
    /// </summary>
    public class LevelEndTextAnim : MonoBehaviour
    {
        [Tooltip("滑入距离(像素), 节点从当前位置左侧这么远的地方滑回原位")]
        public float moveDistance = 100f;

        [Tooltip("单个节点的滑入时长(秒)")]
        public float duration = 0.25f;

        [Tooltip("相邻节点开始播放的间隔(秒)")]
        public float interval = 0.1f;

        [Tooltip("勾选后跳过未激活的子节点")]
        public bool skipInactive = true;

        [Tooltip("按屏幕上下顺序播放(最上面的先动); 关掉则严格按 Hierarchy 顺序")]
        public bool sortByTopToBottom = true;

        private readonly List<RectTransform> _targets = new List<RectTransform>();
        private readonly List<Vector2> _originPos = new List<Vector2>();
        private Sequence _sequence;

        private void Awake()
        {
            CollectTargets();
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }

        /// <summary>
        /// 收集子节点并缓存原始位置。
        /// </summary>
        public void CollectTargets()
        {
            _targets.Clear();
            _originPos.Clear();

            List<KeyValuePair<RectTransform, Vector2>> items = new List<KeyValuePair<RectTransform, Vector2>>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (skipInactive && !child.gameObject.activeSelf)
                {
                    continue;
                }

                if (child is RectTransform rect)
                {
                    items.Add(new KeyValuePair<RectTransform, Vector2>(rect, rect.anchoredPosition));
                }
            }

            //预制体里子节点的排列顺序和视觉上下顺序不一定一致, 按 y 从大到小重排
            if (sortByTopToBottom)
            {
                items.Sort((a, b) => b.Value.y.CompareTo(a.Value.y));
            }

            for (int i = 0; i < items.Count; i++)
            {
                _targets.Add(items[i].Key);
                _originPos.Add(items[i].Value);
            }
        }

        /// <summary>
        /// 播放一次滑入动画; 正在播放时会先停掉再重来。
        /// </summary>
        [Button("测试播放滑入动画", enabledMode: EButtonEnableMode.Playmode)]
        public void Play()
        {
            _sequence?.Kill();

            if (_targets.Count == 0)
            {
                CollectTargets();
            }

            _sequence = DOTween.Sequence().SetTarget(this);
            for (int i = 0; i < _targets.Count; i++)
            {
                RectTransform rect = _targets[i];
                Vector2 origin = _originPos[i];

                //先摆到左侧起点, 到各自的开始时间再滑回原位
                rect.anchoredPosition = new Vector2(origin.x - moveDistance, origin.y);
                _sequence.Insert(interval * i, rect.DOUIMoveX(origin.x, duration));
            }
        }
    }
}
