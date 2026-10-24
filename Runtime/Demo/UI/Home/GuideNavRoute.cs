using UnityEngine;
using UnityEngine.Rendering;

namespace XiaoCao.UI
{
    /// <summary>
    /// 新手引导导航路线: 用 LineRenderer 在角色脚下与目标点之间画一条直线箭头带,
    /// 只有两个点(起点=玩家脚下, 终点=目标点), 中间不插值。
    /// 贴图为项目资源 chevron(">>>>>")箭头, Tile 模式按世界长度平铺,
    /// 通过贴图 offset 滚动实现箭头向目标方向流动, 玩家到达目标附近后整体消失。
    /// 通过 <see cref="Show"/> 触发(引导1完成后指向关卡入口 LevelEnter)。
    ///
    /// 本组件做成 prefab(Assets/_Res/UI/HomeUI/GuideNavRoute.prefab),
    /// 材质与贴图直接序列化引用(GuideNavLine.mat / GuideNavChevron.png), 不走 Resources。
    /// 调用方(HomeGuideController)持有 prefab 引用并传入 <see cref="Show"/>。
    /// 材质用 URP 标准 Unlit(支持 _BaseMap_ST, 粒子版 Unlit 不支持所以滚不动), 加法混合 + 双面。
    /// </summary>
    public class GuideNavRoute : MonoBehaviour
    {
        //线宽(米)
        private const float LineWidth = 0.45f;
        //流动速度(米/秒)
        private const float FlowSpeed = 1.6f;
        //离地高度, 避免与地面 z-fighting
        private const float YOffset = 0.08f;
        //判定到达的水平距离
        private const float ArriveDistance = 2.0f;

        //箭头底色(HDR, 配合加法混合)
        private static readonly Color ArrowColor = new Color(1.7f, 1.35f, 0.5f, 1f);

        private static GuideNavRoute _inst;

        [SerializeField]
        //prefab 上直接引用的材质资源(URP Unlit 加法混合, 贴图已绑在材质上)
        private Material _material;
        [SerializeField]
        //chevron 箭头贴图, 材质实例上兜底重设
        private Texture _chevronTexture;

        private Transform _target;
        private Transform _player;
        private LineRenderer _line;
        private Material _mat;
        //流动相位
        private float _flowOffset;

        /// <summary>
        /// 显示指向 target 的导航路线; prefab 为挂载本组件的预制体(HomeGuideController 持有引用);
        /// 已在显示时仅更换目标点
        /// </summary>
        public static void Show(Transform target, GuideNavRoute prefab)
        {
            if (target == null || prefab == null)
            {
                return;
            }

            if (_inst != null)
            {
                _inst._target = target;
                return;
            }

            _inst = Instantiate(prefab);
            _inst.name = "GuideNavRoute";
            _inst._target = target;
        }

        private void Awake()
        {
            _line = gameObject.AddComponent<LineRenderer>();
            //贴地: TransformZ 让线带的法线朝向 transform.forward, 物体绕 X 转 90° 后 forward 朝下, 线带平铺在地面上
            _line.alignment = LineAlignment.TransformZ;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.textureMode = LineTextureMode.Tile;
            _line.widthMultiplier = LineWidth;
            _line.numCapVertices = 2;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.generateLightingData = false;

            //复制材质实例: 运行时要改颜色/offset, 不能动资产本体
            if (_material != null)
            {
                _mat = new Material(_material);
                if (_chevronTexture != null)
                {
                    _mat.mainTexture = _chevronTexture;
                }
            }
            else
            {
                Debug.LogError("[GuideNavRoute] prefab 未配置材质引用");
            }
            _line.sharedMaterial = _mat;
        }

        private void OnDestroy()
        {
            if (_mat != null)
            {
                Destroy(_mat);
            }
            if (_inst == this)
            {
                _inst = null;
            }
        }

        private void Update()
        {
            //主玩家引用可能因重生/切换失效, 丢失时重新查找
            if (_player == null)
            {
                _player = FindMainPlayer();
                if (_player == null)
                {
                    _line.enabled = false;
                    return;
                }
                _line.enabled = true;
            }

            //目标被销毁(如切场景): 直接结束
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 playerPos = _player.position;
            Vector3 targetPos = _target.position;
            //水平面判定到达(纵向高度差由地形决定, 不能用三维距离)
            float flatDist = Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(targetPos.x, targetPos.z));
            if (flatDist <= ArriveDistance)
            {
                //到达目标: 整条路线消失
                Destroy(gameObject);
                return;
            }

            //只有起点和终点, 每帧直接更新(两点开销极小, 无需限流)
            _line.SetPosition(0, new Vector3(playerPos.x, playerPos.y + YOffset, playerPos.z));
            _line.SetPosition(1, new Vector3(targetPos.x, targetPos.y + YOffset, targetPos.z));

            //流动动画: Tile 模式下 u 从角色端向目标端递增, offset 反向滚动即箭头向目标流动
            if (_mat != null)
            {
                _flowOffset = Mathf.Repeat(_flowOffset + FlowSpeed * Time.deltaTime, 1f);
                _mat.mainTextureOffset = new Vector2(-_flowOffset, 0f);
            }
        }

        /// <summary>
        /// 查找主玩家角色: RoleMgr 中 IsPlayer 且带 MainPlayer 标签的角色,
        /// 避免误把带 Player Tag 的 NPC/支援角色当成起点
        /// </summary>
        private static Transform FindMainPlayer()
        {
            foreach (var role in RoleMgr.Inst.roleDic.Values)
            {
                if (role != null && role.IsPlayer && role.IsLocalMainPlayer)
                {
                    return role.transform;
                }
            }
            return null;
        }
    }
}
