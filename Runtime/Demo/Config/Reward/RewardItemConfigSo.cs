using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/RewardItemConfigSo", fileName = "RewardItemConfigSo")]
    public class RewardItemConfigSo : BaseRewardItemConfigSo
    {
        public float[] ExRate = new[] { 0.1f, 1 };
        public override Item GetRewardItem(int level)
        {
            EBuffType type = GetRandomBuffType(level);
            string id = $"#{((int)type)}";
            Item item = new Item(ItemType.Buff, id);
            return item;
        }


        public EBuffType GetRandomBuffType(int level)
        {
            float p = 0.1f;
            if (level <= 0)
            {
                p = ExRate[0];
            }
            else
            {
                p = ExRate[level % ExRate.Length];
            }

            bool isOther = RandomHelper.GetRandom(p);
            return isOther ? EBuffType.Ex : EBuffType.Nor;
        }
    }
}
