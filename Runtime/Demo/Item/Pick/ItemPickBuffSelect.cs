using System.Collections.Generic;
using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item.Pick
{
    public class ItemPickBuffSelect : MonoBehaviour
    {
        // 玩家触碰时,弹出选择UI,选择后获得 buff
        public List<EBuff> availableBuffs = new List<EBuff>();

        private void OnTriggerEnter(Collider other)
        {
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

            // 将EBuff转换为BuffItem列表
            List<BuffItem> buffItems = new List<BuffItem>();
            foreach (var eBuff in availableBuffs)
            {
                BuffItem buffItem = BuffHelper.CreatBuffItem(eBuff);
                buffItems.Add(buffItem);
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
                player.component.buffControl.AddBuff(selectedBuff);
                // 销毁这个物品
                Destroy(gameObject);
            });
        }

        public List<EBuff> GetBuff()
        {
            return availableBuffs;
        }
    }
}