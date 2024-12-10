using UnityEditor;
using UnityEngine;

namespace XiaoCao
{


    public class SkillBarHud : MonoBehaviour
    {
        public GameObject prefab;
        public SkillBarData BarData => PlayerSaveData.Current.skillBarData;

        public CachePool<SkillSlot> cachePool;

        private ChildPos childPos;

        private Transform slotParent;

        private PlayerAtkTimer _atkTimer;
        public PlayerAtkTimer AtkTimer
        {
            get
            {
                if (_atkTimer == null)
                {
                    _atkTimer = GameDataCommon.LocalPlayer.component.atkTimers;
                }
                return _atkTimer;
            }
        }

        public int SkillCount => GameSetting.SkillCountOnBar;

        public void Init()
        {
            cachePool = new CachePool<SkillSlot>(prefab, SkillCount);
            slotParent = prefab.transform.parent;
            childPos = slotParent.GetComponent<ChildPos>();
            CheckImg();
            gameObject.SetActive(true);

        }


        private void CheckImg()
        {
            ////atkTimer = GameDataCommon.Current.player0.component.atkTimers;

            for (int i = 0; i < SkillCount; i++)
            {
                var solt = cachePool.cacheList[i];
                string skillndex = BarData.onSkill[i];
                solt.transform.SetParent(slotParent);
                solt.gameObject.name = "slot_" + i;
                solt.image.sprite = SpriteResHelper.LoadSkillIcon(skillndex);
            }
            childPos.SetChildPos();
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
                float process = AtkTimer.GetWaitTimeProccess(BarData.onSkill[i]);

                bool isCd = process != 0;
                if (solt.isColdLastFrame && !isCd)
                {
                    Debug.Log($"cd finish! {i}");
                    //播放发光特效
                    solt.PlayEffect();
                }
                if (!solt.isColdLastFrame && isCd)
                {
                    solt.EnterCD();
                }

                solt.isColdLastFrame = isCd;

                //更新进度
                solt.OnUpdate(process);
            }
        }

    }

}
