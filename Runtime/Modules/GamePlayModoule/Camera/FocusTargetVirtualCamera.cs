using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 对焦目标: 启用后临时生成一台 <see cref="CinemachineVirtualCamera"/>, 摆到目标外侧 distance 处、
    /// 并抬到目标上方 fixedHeight 的高度, 用 transitionTime 过渡过去, 保持 focusTime 秒后自动切回游戏相机并关闭。
    /// </summary>
    public class FocusTargetVirtualCamera : MonoBehaviour
    {
        public Transform target;

        [Tooltip("相机到目标的距离")]
        public float distance = 3f;

        [Tooltip("固定高度: 相机最终高度 = 目标高度 + 该值, 正值表示在目标上方")]
        public float fixedHeight = 5f;

        [Tooltip("是否将玩家视角朝向目标: 对焦开始时把玩家相机(CameraController)的注视点设为目标, 结束后不还原")]
        public bool facePlayerCamera = true;

        [Tooltip("切到对焦相机与切回游戏相机的过渡时间(秒)")]
        public float transitionTime = 0.5f;

        [Tooltip("对焦保持时间(秒), 到时间后自动关闭")]
        public float focusTime = 2f;

        /// <summary>对焦相机优先级: 高于场景里的游戏相机即可</summary>
        private const int FocusPriority = 100;

        /// <summary>退出对焦时压到所有相机之下, 让 Brain 自己切回游戏相机</summary>
        private const int IdlePriority = -100;

        private CinemachineVirtualCamera _vcam;
        private CinemachineBrain _brain;

        /// <summary>对焦前的默认混合时间, 退出时还原, 不影响场景里其它切镜头</summary>
        private CinemachineBlendDefinition _originBlend;
        private bool _blendSaved;

        private CancellationTokenSource _cts;

        /// <summary>流程代次: 重播/停止时自增, 让还在 await 的旧流程直接失效</summary>
        private int _runId;

        private void OnEnable()
        {
            Play();
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            if (_vcam != null)
            {
                Destroy(_vcam.gameObject);
            }
        }

        /// <summary>
        /// 开始对焦: 摆好相机 -> 过渡过去 -> 停留 focusTime -> 切回并关闭。重复调用会先中断上一次。
        /// </summary>
        public void Play()
        {
            if (target == null)
            {
                Debug.LogWarning($"{nameof(FocusTargetVirtualCamera)} 未指定 target, 跳对焦。", this);
                return;
            }

            //重播只掐掉上一次的流程, 不降优先级, 否则相机会先切回游戏相机再切过来
            CancelRun();

            _runId++;
            _cts = new CancellationTokenSource();
            RunFocusAsync(_runId, _cts.Token).Forget();
        }

        /// <summary>
        /// 结束对焦: 取消流程, 降优先级让 Brain 按 transitionTime 切回游戏相机, 过渡完再还原混合时间并收掉相机。
        /// </summary>
        public void Stop()
        {
            CancelRun();

            if (_vcam == null || !_vcam.gameObject.activeSelf)
            {
                //本来就没在对焦, 不用等回程
                RestoreBlend();
                return;
            }

            _vcam.Priority = IdlePriority;
            ReturnAfterBlendAsync(_runId).Forget();
        }

        /// <summary>
        /// 取消当前流程并让代次自增, 还在 await 的旧流程会在下一个检查点退出。
        /// </summary>
        private void CancelRun()
        {
            _runId++;

            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// 停止后的收尾: 等回程过渡走完, 再还原混合时间并关闭对焦相机。
        /// 期间又开了新的对焦就跳过, 交给新流程处理。
        /// </summary>
        private async UniTaskVoid ReturnAfterBlendAsync(int runId)
        {
            if (transitionTime > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(transitionTime));
            }

            if (runId != _runId)
            {
                return;
            }

            RestoreBlend();

            if (_vcam != null)
            {
                _vcam.gameObject.SetActive(false);
            }
        }

        private async UniTaskVoid RunFocusAsync(int runId, CancellationToken token)
        {
            try
            {
                //等一帧: OnEnable 早于 CameraMgr / CinemachineBrain 初始化, 这帧后相机位置与 Brain 才可用
                await UniTask.Yield();
                if (runId != _runId)
                {
                    return;
                }

                CinemachineVirtualCamera vcam = EnsureCamera();
                vcam.gameObject.SetActive(true);
                PlaceCamera(vcam);

                //切镜头之前先把玩家相机的注视点转向目标, 切回去时画面才接得上
                AimPlayerCameraToTarget();

                //混合时间要在切换优先级之前写好, Brain 是切换那一刻取值的
                ApplyBlend(transitionTime);
                vcam.Priority = FocusPriority;

                await UniTask.Delay(TimeSpan.FromSeconds(transitionTime), cancellationToken: token);
                if (runId != _runId)
                {
                    return;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(focusTime), cancellationToken: token);
                if (runId != _runId)
                {
                    return;
                }

                //时间到: 降优先级触发切回, 回程也按 transitionTime 走
                vcam.Priority = IdlePriority;
                await UniTask.Delay(TimeSpan.FromSeconds(transitionTime), cancellationToken: token);
                if (runId != _runId)
                {
                    return;
                }

                vcam.gameObject.SetActive(false);
                RestoreBlend();
            }
            catch (OperationCanceledException)
            {
                //被 Stop 取消: 状态已经在 Stop 里清理过
            }
        }

        /// <summary>
        /// 懒创建对焦相机: 不挂父节点, 避免跟着本对象的位移跑; 镜头参数在 PlaceCamera 里对齐主相机。
        /// </summary>
        private CinemachineVirtualCamera EnsureCamera()
        {
            if (_vcam != null)
            {
                return _vcam;
            }

            GameObject go = new GameObject("FocusTargetVCam");
            _vcam = go.AddComponent<CinemachineVirtualCamera>();
            _vcam.Priority = IdlePriority;
            return _vcam;
        }

        /// <summary>
        /// 把相机摆到目标外侧 distance 处, 并抬到目标上方 fixedHeight 的高度:
        /// 平面方向沿用当前主相机看向目标的方向, 所以对焦是"推近"而不是换角度。
        /// </summary>
        private void PlaceCamera(CinemachineVirtualCamera vcam)
        {
            Camera gameCam = CameraMgr.Main;

            //沿用主相机镜头参数, 过渡时视野不会突然变焦
            if (gameCam != null)
            {
                vcam.m_Lens = LensSettings.FromCamera(gameCam);
            }

            Vector3 focus = target.position;
            Vector3 from = gameCam != null ? gameCam.transform.position : transform.position;

            //只取水平分量: 高度单独由 fixedHeight 决定, 否则主相机的高低会顺带改变水平距离
            Vector3 flatDir = from - focus;
            flatDir.y = 0f;

            //主相机正好在目标正上方或与目标重合时没有水平方向, 退回相机朝向在水平面的投影
            if (flatDir.sqrMagnitude < 0.0001f)
            {
                Vector3 forward = gameCam != null ? gameCam.transform.forward : Vector3.forward;
                flatDir = new Vector3(-forward.x, 0f, -forward.z);

                if (flatDir.sqrMagnitude < 0.0001f)
                {
                    flatDir = Vector3.back;
                }
            }

            Transform camT = vcam.transform;
            //distance 为 0 时相机落在目标正上方, 兜一个最小水平距离以免 LookRotation 退化
            camT.position = focus + flatDir.normalized * Mathf.Max(distance, 0.01f) + Vector3.up * fixedHeight;

            Vector3 lookDir = focus - camT.position;
            if (lookDir.sqrMagnitude < 0.0001f)
            {
                lookDir = Vector3.down;
            }

            //几乎垂直俯视时 Vector3.up 和视线共线, 换一个上方向, 免得 LookRotation 报退化
            Vector3 up = Mathf.Abs(Vector3.Dot(lookDir.normalized, Vector3.up)) > 0.999f
                ? flatDir.normalized
                : Vector3.up;

            camT.rotation = Quaternion.LookRotation(lookDir, up);
        }

        /// <summary>
        /// 把玩家相机的注视点指向目标: 跟随点仍然是玩家, 只换 LookAt(CamerAimer.list[0]), 相机还是绕着玩家转,
        /// 但看的方向变成目标(含目标朝向)。按约定结束后不还原, 后续由外部逻辑接管。
        /// </summary>
        private void AimPlayerCameraToTarget()
        {
            if (!facePlayerCamera)
            {
                return;
            }


            ICameraController controller = CameraMgr.Inst.controller;
            if (controller == null)
            {
                return;
            }

            CameraMgr.Inst.LookAtTarget(target);
        }

        /// <summary>
        /// 临时改写 Brain 的默认混合时间, 让切入/切回都按 transitionTime 走; 原值记下来退出时还原。
        /// </summary>
        private void ApplyBlend(float time)
        {
            _brain = GetBrain();
            if (_brain == null)
            {
                return;
            }

            if (!_blendSaved)
            {
                _originBlend = _brain.m_DefaultBlend;
                _blendSaved = true;
            }

            _brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, time);
        }

        private void RestoreBlend()
        {
            if (!_blendSaved)
            {
                return;
            }

            if (_brain != null)
            {
                _brain.m_DefaultBlend = _originBlend;
            }

            _blendSaved = false;
        }

        private static CinemachineBrain GetBrain()
        {
            //BrainCount 为 0 时直接取下标会抛异常, 先判数量
            CinemachineBrain brain = CinemachineCore.Instance.BrainCount > 0
                ? CinemachineCore.Instance.GetActiveBrain(0)
                : null;

            if (brain == null && CameraMgr.Main != null)
            {
                brain = CameraMgr.Main.GetComponent<CinemachineBrain>();
            }

            return brain;
        }
    }
}
