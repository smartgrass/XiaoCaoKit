using NaughtyAttributes;
using UnityEngine;

namespace XiaoCao
{
    public class Test_GameDataView : MonoBehaviour
    {
        public GameDataCommon commonData;
        //过关卡时请空
        public BattleData battleData;

        [Button]
        public void GetCurrentData()
        {
            commonData = GameDataCommon.Current;
            battleData = BattleData.Current;
        }
    }
}
