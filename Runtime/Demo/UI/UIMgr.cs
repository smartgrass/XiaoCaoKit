using UnityEngine;

namespace XiaoCao
{
    public class UIMgr : MonoSingletonPrefab<UIMgr>
    {
        //UI分类:
        //静态ui ->游戏开始就加载    hud
        //面板,窗口 ->动态加载,需要触发才加载   panel



        public BattleView battleHud;

        public LevelSelectionPanel levelSelectionView;

        public SkillBarHud skillBar;

        //懒加载 或 主动加载

        public override void Init()
        {
            base.Init();
            battleHud?.Init();
            skillBar?.Init();
        }

        public void ShowLevelSelectionView()
        {
            levelSelectionView.Show();
        }


    }
}


