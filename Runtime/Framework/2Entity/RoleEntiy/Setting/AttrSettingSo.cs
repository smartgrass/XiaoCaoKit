using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/AttrSettingSo")]
    public class AttrSettingSo: NumMapSo<AttrSetting>
    {
        //不太可能一条曲线调到底, 防止数值失控 需要时再调整 
        public static void FixEnemyAttrByLevel(AttrSetting attr, int lv)
        {
            if (lv > 20)
            {
                    
            }
        }
    }
}
