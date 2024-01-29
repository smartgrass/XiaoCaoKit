using UnityEngine;

namespace XiaoCao
{
    public class UIMgr : MonoBehaviour
    {
        public static UIMgr Inst;

        public BattleUI battleUI;
        //懒加载 或 主动加载


        private void Awake()
        {
            Inst = this;
        }


    }
}


