using UnityEditor;
using UnityEngine;

namespace XiaoCao
{
    public class SkillBarHud : MonoBehaviour
    {
        public GameObject prefab;
        public SkillBarData BarData => PlayerSaveData.Current.skillBarData;

        public CachePool<SkillSlot> cachePool;

        public PlayerAtkTimer atkTimer;

        public int SkillCount => GameSetting.SkillCountOnBar;

        public void Init()
        {
            cachePool = new CachePool<SkillSlot>(prefab, SkillCount);
            CheckImg();
        }


        private void CheckImg()         
        {
            atkTimer = GameDataCommon.Current.player0.component.atkTimers;
            for (int i = 0; i < SkillCount; i++)
            {
                var solt = cachePool.cacheList[i];
                int skillndex = BarData.onSkill[i];
                solt.image.sprite = atkTimer.playerSetting.GetSkillCd(skillndex).sprite;
            }
        }

        public void Update()
        {
            if (GameDataCommon.Current.gameState != GameState.Running)
            {
                return;
            }


            for (int i = 0; i < SkillCount; i++)
            {
                var solt = cachePool.cacheList[i];
                float process = atkTimer.GetProcess(BarData.onSkill[i]);

                bool isCd = process < 1;
                if (solt.isCold && !isCd)
                {
                    Debug.Log($"cd finish! {i}");
                    //播放发光特效
                }
                solt.isCold = isCd;
                //更新进度
                solt.OnUpdate(process);
            }
        }

    }

}
