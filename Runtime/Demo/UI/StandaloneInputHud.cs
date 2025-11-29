using MFPC;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XiaoCao
{


    public class StandaloneInputHud : GameStartMono, IClearCache
    {
        public Transform slotParent;

        public TouchField touchField;

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
                    _atkTimer = GameDataCommon.LocalPlayer.component.atkTimer;
                }
                return _atkTimer;
            }
        }

        private PlayerData0 _playerData;

        private PlayerData0 PlayerData
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

        private bool isTouch;

        public override void OnGameStart()
        {
            base.OnGameStart();
            ClearCache();
            CheckImg();
            slots = slotParent.GetComponentsInChildren<SkillSlot>().ToList();
            //touchField
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void SetTouchShow(bool isTouch)
        {
            slotParent.gameObject.SetActive(!isTouch);
            this.isTouch = isTouch;
        }

        private void ClearCache()
        {
            _atkTimer = null;
            _playerData = null;
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


            UpdateSkillBar();
        }

        private void FixedUpdate()
        {
            PlayerInputData.LocalSwipeDirection = touchField.GetSwipeDirection;
        }

        private void UpdateSkillBar()
        {
            if (isTouch)
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
