using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XiaoCao
{

    [CreateAssetMenu(menuName = "SO/PlayerSettingSo")]
    public class PlayerSettingSo : ScriptableObject
    {
        public PlayerSetting[] playerSetting = new PlayerSetting[1];

        private Dictionary<int, PlayerSetting> psDic;

        private bool isInit { get; set; }

        public PlayerSetting GetPlayerSetting(int roleId)
        {
            if (!isInit)
            {
                psDic = new Dictionary<int, PlayerSetting>();
                foreach (PlayerSetting ps in playerSetting)
                {
                    psDic[ps.roleId] = ps;
                }
                isInit = true;
            }

            var setting = psDic[roleId];

            if (setting == null) 
            {
                return playerSetting[0];
            }
            return setting;

        }
    }

}
