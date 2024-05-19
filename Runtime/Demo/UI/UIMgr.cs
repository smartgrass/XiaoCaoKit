using UnityEngine;

namespace XiaoCao
{
    public class UIMgr : MonoSingletonPrefab<UIMgr>
    {
        //UI分类:
        //静态ui ->游戏开始就加载    hud
        //面板,窗口 ->动态加载,需要触发才加载   panel

        public BattleHud battleHud;

        public SkillBarHud skillBarHud;

        public LevelPanel levelPanel;

        public DebugPanel debugPanel;

        //懒加载 或 主动加载

        public override void Init()
        {
            base.Init();
            battleHud?.Init();
            skillBarHud?.Init();
            debugPanel?.Init();
        }

        public void ShowLevelView()
        {
            levelPanel.Show();
        }


    }
}


