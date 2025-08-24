using System.Collections.Generic;
using System.Linq;

namespace XiaoCao
{
    public class PlayerSaveData
    {
        public static PlayerSaveData LocalSavaData => GameAllData.playerSaveData;

        public int lv;

        public int raceId = 0;

        public string prefabId;

        //技能解锁状态
        public Dictionary<string, int> skillUnlockDic = new Dictionary<string, int>();

        //ItemUI
        public Inventory inventory = new Inventory();
        //持有物
        public List<Item> holdItems = new List<Item>();

        public List<Item> equippedHolyRelics = new List<Item>();

        //反序列化读取的数据, 可能会出现空的现象
        internal void CheckNull()
        {
            // ConfigMgr.LocalSetting.GetBoolValue 暂时不用

            if (inventory == null)
            {
                inventory = new Inventory();
            }
            if (skillUnlockDic == null)
            {
                skillUnlockDic = new Dictionary<string, int>();
            }
            if (string.IsNullOrEmpty(prefabId))
            {
                prefabId = "P_0";
            }
        }

        public void AddSkillLevel(string skillId)
        {
            if (!skillUnlockDic.ContainsKey(skillId))
            {
                skillUnlockDic[skillId] = 1;
            }
            else
            {
                skillUnlockDic[skillId] = skillUnlockDic[skillId] + 1;
            }
        }

        // 存档时调用
        public void SaveEquippedHolyRelics(Dictionary<string, Item> equippedDict)
        {
            equippedHolyRelics = equippedDict.Values.ToList();
        }

        // 读档时调用
        public Dictionary<string, Item> LoadEquippedHolyRelics()
        {
            var dict = new Dictionary<string, Item>();
            foreach (var item in equippedHolyRelics)
            {
                var relic = HolyRelicItem.Create(item);
                dict[relic.modName] = item;
            }
            return dict;
        }

        public static void Sava()
        {
            SaveMgr.SaveData(PlayerSaveData.LocalSavaData);
        }
    }


}
