using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/PlayerSettingSo")]
    public class PlayerSettingSo : ScriptableObject
    {
        public PlayerSetting playerSetting = new PlayerSetting();
    }

}
