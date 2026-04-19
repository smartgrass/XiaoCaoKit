using UnityEngine;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class LevelEndTrigger : MonoBehaviour, IMapMsgSender
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(Tags.PLAYER))
            {
                return;
            }

            var idRole = other.GetComponent<IdRole>();
            if (!idRole)
            {
                return;
            }

            GameMgr.Inst.ShowLevelEndDialog();
        }
    }
}
