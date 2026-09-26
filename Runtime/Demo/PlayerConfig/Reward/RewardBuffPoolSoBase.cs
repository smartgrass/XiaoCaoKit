using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public abstract class RewardBuffPoolSoBase : ScriptableObject, IBuffPool
    {
        public abstract List<EBuff> GetRandomBuffs(int count);
    }
}