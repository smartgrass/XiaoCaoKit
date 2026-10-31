using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class TaskTriggerBase : HomeTriggerBase
    {
        //红点节点名: 与 UIOpenBtn 的做法一致, 按名字找, 场景里不用额外绑定引用
        private const string RedDotName = "BtnRedDot";

        //红点轮询间隔: 任务进度来自玩法(升级技能/通关/获得圣器), 没有统一的事件可监听
        private const float RedDotInterval = 1f;

        //任务入口红点, 挂在 TaskTrigger 下的 Canvas_Talk 子节点里
        private GameObject _redDot;

        //缺少红点节点时只警告一次
        private bool _redDotWarned;

        public override void OnEnable()
        {
            base.OnEnable();

            _redDot = transform.FindChildEx(RedDotName)?.gameObject;
            if (_redDot == null)
            {
                if (!_redDotWarned)
                {
                    _redDotWarned = true;
                    Debuger.LogWarning($"--- {name} 下没有 {RedDotName} 节点, 任务红点不会显示", this);
                }
            }
            else
            {
                //读档完成前先隐藏, 避免开局瞬间露出红点
                _redDot.SetActive(false);
            }

            RefreshRedDot();
            InvokeRepeating(nameof(RefreshRedDot), RedDotInterval, RedDotInterval);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(RefreshRedDot));
        }

        /// <summary>
        /// 刷新红点: 任意分类下存在可领取任务就显示
        /// </summary>
        private void RefreshRedDot()
        {
            //没有红点节点, 或存档还没就绪(读不到任务记录)时跳过, 等下一轮轮询
            if (_redDot == null || GameAllData.playerSaveData == null)
            {
                return;
            }

            _redDot.SetActive(TaskMgr.HasAnyClaimable());
        }

        public override void TriggerSucceed()
        {
            //打开任务界面
            UIMgr.Inst.ShowView(UIPanelType.TaskPanel);
        }
    }
}