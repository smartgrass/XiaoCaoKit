using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/RewardBuffPoolSo", fileName = "RewardBuffPoolSo")]
    public class RewardBuffPoolSo : RewardBuffPoolSoBase
    {
        //ExBuff概率
        public float exRate = 0.1f;

        public EBuffType GetRandomBuffType()
        {
            bool isOther = RandomHelper.GetRandom(exRate);
            return isOther ? EBuffType.Ex : EBuffType.Nor;
        }

        public override List<EBuff> GetRandomBuffs(int count)
        {
            List<EBuff> buffs = new List<EBuff>();
            HashSet<EBuff> usedBuffs = new HashSet<EBuff>();

            for (int i = 0; i < count; i++)
            {
                EBuffType eBuffType = GetRandomBuffType();
                EBuff buff;

                // 确保不重复获取相同的Buff
                int attempts = 0;
                const int maxAttempts = 10;
                do
                {
                    buff = BuffHelper.GetRandomBuff(eBuffType);
                    attempts++;

                    // 如果尝试次数过多，则跳出循环避免无限循环
                    if (attempts >= maxAttempts)
                    {
                        break;
                    }
                } while (usedBuffs.Contains(buff));

                buffs.Add(buff);
                usedBuffs.Add(buff);
            }

            return buffs;
        }
    }


    public interface IBuffPool
    {
        //获取随机的Buff
        public List<EBuff> GetRandomBuffs(int count);
    }
}