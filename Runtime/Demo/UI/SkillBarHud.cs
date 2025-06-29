﻿using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace XiaoCao
{


    public class SkillBarHud : GameStartMono, IClearCache
    {
        public Transform slotParent;
        [ReadOnly]
        public List<SkillSlot> slots;
        //private ChildPos childPos;

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

        public PlayerData0 _playerData;
        public PlayerData0 PlayerData
        {
            get
            {
                if (_playerData == null)
                {
                    _playerData = GameDataCommon.LocalPlayer.playerData;
                }
                return _playerData;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void ClearCache()
        {
            _atkTimer = null;
            _playerData =null;
        }

        public override void OnGameStart()
        {
            base.OnGameStart();
            ClearCache();
            CheckImg();
            slots = slotParent.GetComponentsInChildren<SkillSlot>().ToList();
        }


        private void CheckImg()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var solt = slots[i];
                string skillndex = PlayerData.GetBarSkillId(i);
                solt.image.sprite = SpriteResHelper.LoadSkillIcon(skillndex);
            }
            //childPos.SetChildPos();
        }

        public void Update()
        {
            if (GameDataCommon.Current.gameState != GameState.Running)
            {
                return;
            }


            for (int i = 0; i < slots.Count; i++)
            {
                var solt = slots[i];
                float process = AtkTimer.GetWaitTimeProccess(PlayerData.GetBarSkillId(i));

                bool isCd = process != 0;
                if (solt.isColdLastFrame && !isCd)
                {
                    Debug.Log($"cd finish! {i}");
                    //播放发光特效
                    solt.CdFinish();
                }
                if (!solt.isColdLastFrame && isCd)
                {
                    solt.CdEnter();
                }

                solt.isColdLastFrame = isCd;

                //更新进度
                solt.OnUpdate(process);
            }
        }

    }

}
