using System.Collections;
using UnityEngine;
using XiaoCao.UI;

namespace XiaoCao
{
    public class HomeMapControl : MonoBehaviour
    {
        public Transform startPoint;

        //等存档/UI就绪的上限(秒)
        private const float DataReadyTimeout = 5f;
        //等资源包初始化的上限(秒), 主城直启时资源包还在初始化
        private const float ResReadyTimeout = 60f;
        //创建引导的重试间隔与次数
        private const float CreateRetryInterval = 0.5f;
        private const int CreateRetryCount = 6;

        private void Start()
        {
            //主城加载后挂载新手引导, 引导根节点随场景一起销毁
            StartCoroutine(CreateGuideWhenReady());
        }

        private IEnumerator CreateGuideWhenReady()
        {
            //等一帧, 避免在场景 Awake 阶段提前创建 UIMgr
            yield return null;

            //1.存档与 UIMgr 就绪
            float time = 0f;
            while (time < DataReadyTimeout)
            {
                if (GameAllData.playerSaveData != null && UIMgr.Inst != null && UIMgr.Inst.topCanvas != null)
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
                yield return null;
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
