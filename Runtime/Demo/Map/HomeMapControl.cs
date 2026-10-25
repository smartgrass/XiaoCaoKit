using System.Collections;
using UnityEngine;
using XiaoCao.UI;

namespace XiaoCao
{
    /// <summary>
    /// 主城地图控制: 继承 <see cref="GameStartMono"/>, 等游戏状态进入 Running 后再挂载新手引导。
    /// 不能直接写在 Start 里, 否则会抢在启动流程(加载地图/创建玩家/初始化 UIMgr)之前执行。
    /// </summary>
    public class HomeMapControl : GameStartMono
    {
        public Transform startPoint;

        //等存档/UI就绪的上限(秒)
        private const float DataReadyTimeout = 5f;
        //等资源包初始化的上限(秒), 主城直启时资源包还在初始化
        private const float ResReadyTimeout = 60f;
        //创建引导的重试间隔与次数
        private const float CreateRetryInterval = 0.5f;
        private const int CreateRetryCount = 6;

        public override void OnGameStart()
        {
            //Pause -> Running 会再次触发, 只跑一次
            if (isGameStarted)
            {
                return;
            }

            base.OnGameStart();

            //主城加载后挂载新手引导, 引导根节点随场景一起销毁
            StartCoroutine(CreateGuideWhenReady());
        }

        private IEnumerator CreateGuideWhenReady()
        {
            //等一帧, 让启动流程把 UIMgr 等基础对象拉起来
            yield return null;

            //1.存档与 UIMgr 就绪: UIMgr.Inst 会自动创建实例, 这里只能判断"是否已存在"
            float time = 0f;
            while (time < DataReadyTimeout)
            {
                if (GameAllData.playerSaveData != null && UIMgr.HasInst && UIMgr.Inst.topCanvas != null)
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
                yield return null;
            }

            if (GameAllData.playerSaveData == null || !UIMgr.HasInst || UIMgr.Inst.topCanvas == null)
            {
                Debuger.LogError("[Guide] 等待存档/UIMgr 就绪超时, 引导未创建");
                yield break;
            }

            //2.资源包(YooAsset)初始化完成: manifest 未激活时 CheckLocationValid 会抛异常
            time = 0f;
            while (time < ResReadyTimeout)
            {
                if (HomeGuideController.IsResReady())
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!HomeGuideController.IsResReady())
            {
                Debuger.LogError("[Guide] 等待资源包初始化超时, 引导未创建");
                yield break;
            }

            //3.资源刚就绪时manifest可能还没完全生效, 失败则间隔重试
            for (int i = 0; i < CreateRetryCount; i++)
            {
                if (HomeGuideController.EnsureCreated())
                {
                    yield break;
                }
                yield return new WaitForSecondsRealtime(CreateRetryInterval);
            }

            Debuger.LogError("[Guide] 引导预制体创建失败");
        }

        public Vector3 GetStartPos()
        {
            if (startPoint)
            {
                return startPoint.position;
            }

            return Vector3.zero;
        }
    }
}
