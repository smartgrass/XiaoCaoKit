using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/CommonSo")]
    public class CommonSo: ScriptableObject
    {
        public AttrSetting playerSetting;
        public AttrSetting enemySetting;
    }
}
