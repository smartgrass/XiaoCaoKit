using UnityEngine;

namespace XiaoCao
{
    public class UIMgr : MonoSingletonPrefab<UIMgr>
    {
        public BattleView battleUI;

        public LevelSelectionView levelSelectionView;

        public SkillBar skillBar;
        //懒加载 或 主动加载


        public override void Init()
        {
            base.Init();
            battleUI?.Init();
            levelSelectionView?.Init();
            skillBar?.Init();
        }

        public void ShowLevelSelectionView()
        {
            levelSelectionView.gameObject.SetActive(true);
        }


    }
}


