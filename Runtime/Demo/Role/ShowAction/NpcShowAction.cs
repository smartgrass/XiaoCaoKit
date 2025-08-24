using NaughtyAttributes;
using System.Collections;
using TEngine;
using UnityEngine;

namespace XiaoCao
{

    public class NpcShowAction : BaseShowAction
    {
        public Npc npc;

        public override CharacterController Cc => npc.Cc;

        private void Awake()
        {
            if (!npc)
            {
                npc = GetComponent<Npc>();
            }
        }

        public override void OnGameStart()
        {
            base.OnGameStart();
            StartCoroutine(IETaskRun());
        }

        [Button]
        void ReStartTask()
        {
            StopAllCoroutines();
            StartCoroutine(IETaskRun());
        }
    }

}
