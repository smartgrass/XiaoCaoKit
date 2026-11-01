using UnityEngine;

namespace XiaoCao
{
    /// <summary>
    /// 炮台瞄准辅助线: 从炮口连到准星命中的位置, 只在玩家操控炮台期间显示。
    /// 线条走世界空间, 落点由 <see cref="Turret"/> 每帧算好传进来, 所以线头永远压在准星上。
    /// </summary>
    public class TurretAimLine : MonoBehaviour
    {
        [Tooltip("线条组件, 为空则不显示")]
        public LineRenderer line;

        [Header("样式")]
        [Tooltip("锁定屏幕像素宽度: 近处不会粗成一根楔子, 远处也不会细到看不见")]
        public bool keepScreenWidth = true;

        [Tooltip("锁定屏幕宽度时线条在屏幕上的粗细(像素)")]
        public float lineWidthPixels = 3f;

        [Tooltip("不锁屏幕宽度时炮口一端的宽度(米)")]
        public float startWidth = 0.025f;

        [Tooltip("不锁屏幕宽度时落点一端的宽度(米)")]
        public float endWidth = 0.02f;

        [Tooltip("线条起点沿瞄准方向前移的距离(米); 炮口就在视野正下方, 不前移的话近处会被透视拉成一根粗楔子")]
        public float startForward = 1.2f;

        [Tooltip("炮口一端颜色")]
        public Color startColor = new Color(1f, 0.45f, 0.3f, 0.75f);

        [Tooltip("落点一端颜色, 透明度调低让线尾自然消失")]
        public Color endColor = new Color(1f, 0.45f, 0.3f, 0.1f);

        [Header("虚线")]
        [Tooltip("按屏幕像素等距分布: 近处不会拖成长条, 远处也不会糊成点阵; 关掉则按世界空间等距(近疏远密, 更像真实激光)")]
        public bool dashScreenUniform = true;

        [Tooltip("屏幕等距时, 每隔多少像素重复一个虚线周期(实线+空隙)")]
        public float dashPixels = 34f;

        [Tooltip("世界等距时, 一个虚线周期的长度(米)")]
        public float dashCycleWorld = 0.75f;

        [Tooltip("一个周期里实线占的比例, 1 = 实线, 0 = 全空")]
        [Range(0f, 1f)]
        public float dashFill = 0.6f;

        [Tooltip("虚线往落点方向流动的速度(周期/秒), 0 = 静止")]
        public float dashFlowSpeed = 1.2f;

        [Header("落点标记")]
        [Tooltip("落点处的标记节点(可选), 会摆到命中的位置并朝向相机")]
        public Transform hitMarker;

        [Tooltip("命中敌人时标记的缩放, 没打中目标时用 hitMarkerMissScale")]
        public float hitMarkerFightScale = 1f;

        [Tooltip("没打到目标(只打到墙或空处)时标记的缩放")]
        public float hitMarkerMissScale = 0.5f;

        private Camera mainCamera;

        //运行时材质实例: 虚线参数每帧都在变, 不能改到资产本体
        private Material dashMat;

        //流动相位(单位: 虚线周期)
        private float dashScroll;

        private void Awake()
        {
            if (line == null)
            {
                Debug.LogWarning($"[TurretAimLine] {name} 没有绑定 LineRenderer, 瞄准线不显示", this);
                enabled = false;
                return;
            }

            line.useWorldSpace = true;
            line.positionCount = 2;
            line.enabled = false;

            if (hitMarker != null)
            {
                hitMarker.gameObject.SetActive(false);
            }

            EnsureDashMaterial();
            ApplyStyle();
        }

        private void OnDestroy()
        {
            if (dashMat != null)
            {
                Destroy(dashMat);
            }
        }

        /// <summary>把 Inspector 上的颜色/宽度同步到线条(运行时改参数也能立刻生效)</summary>
        private void ApplyStyle()
        {
            if (!keepScreenWidth)
            {
                line.startWidth = startWidth;
                line.endWidth = endWidth;
            }

            line.startColor = startColor;
            line.endColor = endColor;
            line.useWorldSpace = true;
            line.numCapVertices = 2;
            //Stretch 模式 uv.x 沿整条线从 0 到 1, 虚线靠 shader 在这个 uv 上做条纹
            line.textureMode = LineTextureMode.Stretch;
            line.generateLightingData = false;
            //始终面向相机, 侧着看也不会变成一条细缝
            line.alignment = LineAlignment.View;
        }

        /// <summary>显示瞄准线</summary>
        public void Show()
        {
            if (line == null)
            {
                return;
            }

            ApplyStyle();
            line.enabled = true;
        }

        /// <summary>隐藏瞄准线</summary>
        public void Hide()
        {
            line.enabled = false;

            if (hitMarker != null)
            {
                hitMarker.gameObject.SetActive(false);
            }
        }

        /// <summary>取主相机, 拿不到时每帧重试一次(切关卡会换相机)</summary>
        private void EnsureCamera()
        {
            if (mainCamera == null)
            {
                mainCamera = CameraMgr.Main;
            }
        }

        /// <summary>
        /// 按相机距离反算世界宽度, 让线条在屏幕上始终是 lineWidthPixels 像素粗。
        /// 炮台视角是长焦(FOV 很小), 固定世界宽度在近处会被透视放大成很粗的一段
        /// </summary>
        private void ApplyScreenWidth(Vector3 start, Vector3 end)
        {
            EnsureCamera();

            if (mainCamera == null)
            {
                return;
            }

            //一个像素在 1 米远的地方对应多少世界尺寸
            float fovRad = mainCamera.fieldOfView * Mathf.Deg2Rad;
            float onePixel = 2f * Mathf.Tan(fovRad * 0.5f) / Mathf.Max(1, mainCamera.pixelHeight);

            Vector3 camPos = mainCamera.transform.position;
            line.startWidth = Vector3.Distance(camPos, start) * onePixel * lineWidthPixels;
            line.endWidth = Vector3.Distance(camPos, end) * onePixel * lineWidthPixels;
        }

        /// <summary>
        /// 复制一份材质实例给 LineRenderer, 虚线参数写到实例上
        /// </summary>
        private void EnsureDashMaterial()
        {
            if (dashMat != null)
            {
                return;
            }

            Material src = line.sharedMaterial;
            if (src == null)
            {
                return;
            }

            dashMat = new Material(src);
            dashMat.name = src.name + " (Instance)";
            //断口的柔和程度只跟材质有关, 设一次就够
            dashMat.SetFloat("_Softness", 1.5f);
            line.sharedMaterial = dashMat;
        }

        /// <summary>
        /// 刷新虚线密度与流动相位。
        /// 屏幕等距模式下密度按线条在屏幕上的像素长度算, 世界等距模式按世界长度算,
        /// 两种模式都只改材质的几个 float, 不会重建几何
        /// </summary>
        private void UpdateDash(Vector3 start, Vector3 end, float length)
        {
            if (dashMat == null)
            {
                return;
            }

            dashMat.SetFloat("_Fill", Mathf.Clamp01(dashFill));

            if (dashScreenUniform)
            {
                EnsureCamera();

                if (mainCamera != null)
                {
                    Vector3 screenA = mainCamera.WorldToScreenPoint(start);
                    Vector3 screenB = mainCamera.WorldToScreenPoint(end);
                    float screenLength = Vector2.Distance(screenA, screenB);
                    float period = Mathf.Max(4f, dashPixels);

                    dashMat.SetFloat("_ScreenUniform", 1f);
                    dashMat.SetFloat("_Density", Mathf.Max(1f, screenLength / period));
                    dashMat.SetFloat("_DistStart", Vector3.Distance(mainCamera.transform.position, start));
                    dashMat.SetFloat("_DistEnd", Vector3.Distance(mainCamera.transform.position, end));
                }
            }
            else
            {
                dashMat.SetFloat("_ScreenUniform", 0f);
                dashMat.SetFloat("_Density", Mathf.Max(1f, length / Mathf.Max(0.05f, dashCycleWorld)));
            }

            if (Mathf.Approximately(dashFlowSpeed, 0f))
            {
                return;
            }

            //相位减小时条纹朝落点方向移动
            dashScroll = Mathf.Repeat(dashScroll - dashFlowSpeed * Time.deltaTime, 1f);
            dashMat.SetFloat("_Scroll", dashScroll);
        }

        /// <summary>
        /// 更新线条: 从炮口连到落点
        /// </summary>
        /// <param name="start">炮口位置</param>
        /// <param name="end">准星命中位置</param>
        /// <param name="isTarget">命中的是不是敌对角色, 决定落点标记的大小</param>
        public void SetLine(Vector3 start, Vector3 end, bool isTarget)
        {
            Vector3 dir = end - start;
            float length = dir.magnitude;

            //起点往前让一段, 避免贴着镜头的部分被透视放大得过于夸张
            if (startForward > 0f && length > startForward * 2f)
            {
                start += dir / length * startForward;
            }

            if (keepScreenWidth)
            {
                ApplyScreenWidth(start, end);
            }

            line.SetPosition(0, start);
            line.SetPosition(1, end);

            UpdateDash(start, end, Vector3.Distance(start, end));

            if (hitMarker == null)
            {
                return;
            }

            hitMarker.gameObject.SetActive(true);
            hitMarker.position = end;
            hitMarker.localScale = Vector3.one * (isTarget ? hitMarkerFightScale : hitMarkerMissScale);

            EnsureCamera();

            if (mainCamera != null)
            {
                hitMarker.rotation = mainCamera.transform.rotation;
            }
        }
    }
}
