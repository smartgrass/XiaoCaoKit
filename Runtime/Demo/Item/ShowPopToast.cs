using System;
using XiaoCao;
using XiaoCao.UI;

namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class ShowPopToast : MonoExecute
    {
        public bool isAutoStart;

        private void OnEnable()
        {
            if (isAutoStart)
            {
                Execute();
            }
        }

        public string popToastKey;

        public override void Execute()
        {
            UIMgr.PopToastKey(popToastKey);
        }
    }
}