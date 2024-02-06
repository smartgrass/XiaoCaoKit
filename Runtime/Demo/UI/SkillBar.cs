using UnityEditor;
using UnityEngine;

namespace XiaoCao
{
    public class SkillBar : MonoBehaviour
    {
        public GameObject prefab;
        public skillBarData barData => PlayerSaveData.Current.skillBarData;

        public CachePool<SkillSlot> cachePool;

        public PlayerAtkTimer atkTimer;

        public int SkillCount => GameSetting.SkillCountOnBar;

        public void Init()
        {
            cachePool = new CachePool<SkillSlot>(prefab);
            cachePool.UpdateCachedAmount(SkillCount);
            CheckImg();
        }


        private void CheckImg()
        {
            atkTimer = GameDataCommon.Current.player0.component.atkTimers;
            for (int i = 0; i < SkillCount; i++)
            {
                var solt = cachePool.cacheList[i];
                int skillndex = barData.onSkill[i];
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
                float process = atkTimer.GetProcess(barData.onSkill[i]);

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

    public class skillBarData
    {
        public int[] onSkill = new int[GameSetting.SkillCountOnBar];
    }
}
