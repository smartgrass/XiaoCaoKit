using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item.Pick
{
    public class ItemPickBuffSelect : MonoBehaviour
    {
        public int getCount = 2;

        public RewardBuffPoolSo buffPool;

        [Header("优先使用buffPool")] public List<EBuff> simplePool = new List<EBuff>();

        public bool forceShowUI;
        
        private bool _isPicked;

        private void OnTriggerEnter(Collider other)
        {
            if (_isPicked)
            {
                return;
            }

            if (PlayerHelper.IsLocalPlayerCollider(other, out var player))
            {
                OnPlayerEnter(player);
            }
        }

        private void OnPlayerEnter(Player0 player)
        {
            if (player != GameDataCommon.LocalPlayer)
            {
                return;
            }


            var buffs = GetBuff();
            // 将EBuff转换为BuffItem列表
            List<BuffItem> buffItems = new List<BuffItem>();
            foreach (var eBuff in buffs)
            {
                BuffItem buffItem = BuffHelper.CreatBuffItem(eBuff);
                buffItems.Add(buffItem);
            }

            //1个则直接获得
            if (buffs.Count == 1 && !forceShowUI)
            {
                EffectBuffAndHide(player, buffItems[0]);
                return;
            }

            // 显示选择界面
            ShowBuffSelectionUI(buffItems, player);
        }

        private void ShowBuffSelectionUI(List<BuffItem> buffItems, Player0 player)
        {
            // 这里我们假设UIMgr会处理BuffSelectPanel的创建和管理
            UIMgr.Inst.ShowBuffSelection(buffItems, (selectedBuff) =>
            {
                // 玩家选择了Buff，应用到角色上
                EffectBuffAndHide(player, selectedBuff);
            });
        }

        private void EffectBuffAndHide(Player0 player, BuffItem selectedBuff)
        {
            PlayerHelper.AddBuff(player.id, selectedBuff, true);
            // player.component.buffControl.AddBuff(selectedBuff);
            _isPicked = true;
            transform.DOScale(Vector3.zero, 0.3f).OnComplete(() => { Destroy(gameObject); });

            string path = "Assets/_Res/Audio/Effect/heal.mp3";
            AudioClip audioClip = ResMgr.LoadAseet(path) as AudioClip;
            SoundMgr.Inst.PlayClip(audioClip);
        }

        public List<EBuff> GetBuff()
        {
            if (buffPool)
            {
                return buffPool.GetRandomBuffs(getCount);
            }

            return simplePool;
        }

        [Button]
        void TestGetBuff()
        {
            simplePool = buffPool.GetRandomBuffs(getCount);
        }
    }
}