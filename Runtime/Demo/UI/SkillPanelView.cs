using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao
{

    public class SkillPanelView: MonoBehaviour
    {
/*
操作: 右上角开启编辑模式, 单点选入; 已装备栏, 右上角带x, 卸下
分解&循序渐进:
1.SkillCell 显示技能图标, 点击时右侧显示描述&升级按钮
2.开启编辑模模式, 单选加入, 不允许空位


*/
        public Transform prefabs;
        public RectTransform equippedBuffContainer; // 已装备buff的容器
        public RectTransform unequippedBuffContainer; // 未装备buff的容器

        public RectTransform textContainer;

        public GameObject buffItemPrefab; // BuffItem的Prefab
        public TextMeshProUGUI buffTextPrefab; // BuffItem的Prefab


        public Button switchBtn;
        public TextMeshProUGUI buffTitle;

        private PlayerBuffs playerBuffs;


        private AssetPool textPool;
        private AssetPool buffCellPool;


        public SkillUpgradeView skillUpgradeView;//暂时无用


    }

}