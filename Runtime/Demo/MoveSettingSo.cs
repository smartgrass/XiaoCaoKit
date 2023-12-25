using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/MoveSettingSo")]
    public class MoveSettingSo : ScriptableObject
    {
        public MoveSetting moveSetting = new MoveSetting();
    }

}
