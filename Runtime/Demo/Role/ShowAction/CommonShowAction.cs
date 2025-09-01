using UnityEngine;

namespace XiaoCao
{
    public class CommonShowAction : BaseShowAction
    {
        public override void OnGameStart()
        {
            base.OnGameStart();
            StartCoroutine(IETaskRun());
        }

    }
}