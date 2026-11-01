using Cinemachine;
using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 炮台第一人称瞄准相机: 用一个 Cinemachine 虚拟相机提供画面,
    /// 进入/退出炮台视角的过渡由 CinemachineBrain 的混合表
    /// (Resources/Camera/MainCameraBlends.asset) 决定, 按虚拟相机名 TurretAimCamera 配置即可。
    /// 虚拟相机不带 Body/Aim 组件, 画面位姿就是它自身的 transform, 由炮台每帧写入,
    /// 所以准心方向与炮口方向完全一致。
    /// </summary>
    public class TurretAimCamera
    {
        /// <summary>虚拟相机名, 与 MainCameraBlends 里的混合项对应</summary>
        public const string VCamName = "TurretAimCamera";

        /// <summary>待机优先级, 低于主相机(10)时不接管画面</summary>
        private const int IdlePriority = 0;

        private static TurretAimCamera current;

        private CinemachineVirtualCamera vcam;

        /// <summary>是否有炮台正在接管主相机, 供相机模块让位</summary>
        public static bool IsActive => current != null;

        /// <summary>当前是否处在炮台视角</summary>
        public bool IsOn { get; private set; }

        /// <summary>当前虚拟相机, 未创建时为 null</summary>
        public CinemachineVirtualCamera VCam => vcam;

        /// <summary>
        /// 接管主相机: 把虚拟相机优先级提到主相机之上, CinemachineBrain 会按混合表过渡过来
        /// </summary>
        /// <param name="parent">虚拟相机挂在哪个节点下, 一般传炮台根节点</param>
        /// <param name="position">首帧位姿, 避免过渡从原点飞过来</param>
        /// <param name="rotation">首帧朝向</param>
        /// <param name="fieldOfView">焦距, 20 为长焦</param>
        /// <param name="priority">接管期间优先级, 要大于主相机</param>
        public void Begin(Transform parent, Vector3 position, Quaternion rotation, float fieldOfView, int priority)
        {
            EnsureVCam(parent);

            vcam.m_Lens.FieldOfView = fieldOfView;

            //先把位姿写好再提优先级, 混合的第一帧才是正确位置
            Apply(position, rotation);
            vcam.Priority = priority;

            current = this;
            IsOn = true;
        }

        /// <summary>交还主相机: 降低优先级, CinemachineBrain 会混合回原来的虚拟相机</summary>
        public void End()
        {
            if (!IsOn)
            {
                return;
            }

            IsOn = false;
            if (current == this)
            {
                current = null;
            }

            if (vcam != null)
            {
                vcam.Priority = IdlePriority;
            }
        }

        /// <summary>写入虚拟相机位姿, 炮台每帧在 LateUpdate 调用</summary>
        public void Apply(Vector3 position, Quaternion rotation)
        {
            if (vcam == null)
            {
                return;
            }

            vcam.transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// 首次接管时创建虚拟相机: 故意不加 Body/Aim 组件,
        /// Cinemachine 会直接用虚拟相机自身的 transform 作为画面位姿
        /// </summary>
        private void EnsureVCam(Transform parent)
        {
            if (vcam != null)
            {
                return;
            }

            GameObject go = new GameObject(VCamName);
            go.transform.SetParent(parent, false);

            vcam = go.AddComponent<CinemachineVirtualCamera>();
            vcam.Priority = IdlePriority;
            vcam.m_StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Never;

            //远近裁面与主相机保持一致, 避免过渡时边缘跳变
            vcam.m_Lens.NearClipPlane = 0.2f;
            vcam.m_Lens.FarClipPlane = 500f;
            vcam.m_Lens.FieldOfView = 20f;
        }
    }
}
