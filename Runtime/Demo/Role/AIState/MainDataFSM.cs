using UnityEngine;

namespace XiaoCao
{

    public class MainDataFSM : MainFSM
    {
        [XCLabel("描述")]
        public string des;

        public AISetting setting;

        [XCLabel("移动模式")]
        [Tooltip("默认模式: 直接朝目标走; 寻路模式: 用地图里的通道连线点(PassPointCollector)绕开墙体")]
        public EnemyMoveMode moveMode = EnemyMoveMode.Default;

        [XCLabel("寻路: 接入半径")]
        [Tooltip("寻路模式专用. 以自身/目标为中心, 该半径内的通道连线点都算出入口. " +
                 "半径外距离最近的那一个也必定算可达, 所以不会再出现接不上图而原地站死")]
        public float pathFarDistance = 18f;
    }
}
