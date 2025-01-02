using UnityEngine;

namespace XiaoCao
{
    [CreateAssetMenu(menuName = "SO/RewardItemConfigSo", fileName = "RewardItemConfigSo")]
    public class RewardItemConfigSo : BaseRewardItemConfigSo
    {
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
            if (level == 1)
            {
                p = 0.5f;
            }
            else if (level > 1)
            {
                p = 1;
            }
            bool isOther = RandomHelper.GetRandom(p);
            return isOther ? EBuffType.Other : EBuffType.Nor;
        }
    }
}
