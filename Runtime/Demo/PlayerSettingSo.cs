using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace XiaoCao
{

    [CreateAssetMenu(menuName = "SO/PlayerSettingSo")]
    public class PlayerSettingSo : ScriptableObject
    {
        public PlayerSetting[] playerSetting = new PlayerSetting[1];

        private Dictionary<int, PlayerSetting> psDic;

        public PlayerSetting GetPlayerSetting(int roleId)
        {
            if (null == psDic)
            {
                psDic = new Dictionary<int, PlayerSetting>();
                foreach (PlayerSetting ps in playerSetting)
                {
                    psDic[ps.roleId] = ps;
                }
            }

            psDic.TryGetValue(roleId, out PlayerSetting setting);

            if (setting == null) 
            {
                Debug.Log($"--- roleId {roleId} null->default");
                return playerSetting[0];
            }
            return setting;

        }
    }

}
