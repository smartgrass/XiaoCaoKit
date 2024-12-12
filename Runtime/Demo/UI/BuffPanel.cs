using UnityEngine;

namespace XiaoCao
{
    public class BuffPanel : SubPanel
    {
        public override void Init()
        {
            
        }

        public RectTransform equippedBuffContainer; // 已装备buff的容器
        public RectTransform unequippedBuffContainer; // 未装备buff的容器
        public GameObject buffItemTemplate; // BuffItem的Prefab

        private PlayerBuffs playerBuffs;
        private ObjectPool<GameObject> pool;

        void Start()
        {
            // 初始化PlayerBuffs数据（这里应该是从玩家数据或其他地方获取的）
            playerBuffs = new PlayerBuffs();
            // 为了示例，我们手动添加一些BuffItem到列表中
            playerBuffs.EquippedBuffs.Add(new BuffItem { /* 初始化属性 */ });
            playerBuffs.EquippedBuffs.Add(new BuffItem { /* 初始化属性 */ });
            // ... 添加更多已装备和未装备的BuffItem

            // 更新UI以显示buff
            UpdateBuffDisplay();
        }

        public void UpdateBuffDisplay()
        {
            // 清空容器中的现有buff
            ClearContainer(equippedBuffContainer);
            ClearContainer(unequippedBuffContainer);

            // 显示已装备的buff
            for (int i = 0; i < playerBuffs.EquippedBuffs.Count && i < playerBuffs.MaxEquipped; i++)
            {
                InstantiateBuffItem(equippedBuffContainer, playerBuffs.EquippedBuffs[i]);
            }

            // 显示未装备的buff
            for (int i = 0; i < playerBuffs.UnequippedBuffs.Count; i++)
            {
                InstantiateBuffItem(unequippedBuffContainer, playerBuffs.UnequippedBuffs[i]);
            }
        }

        private void InstantiateBuffItem(RectTransform container, BuffItem buffItem)
        {
            // 在容器中实例化BuffItem的Prefab
            GameObject newBuffItem = GetFromPool();
            newBuffItem.transform.SetParent(container.transform, false);
            // 设置BuffItem的属性和显示内容（这里需要您自己实现具体的设置逻辑）
            // 例如：newBuffItem.GetComponent<BuffItemDisplay>().SetBuffItem(buffItem);
            // 注意：BuffItemDisplay是一个假设的脚本，用于处理BuffItem的显示逻辑

            // 确保新实例正确放置在容器中（这通常是由布局组自动处理的）
        }

        private void ClearContainer(RectTransform container)
        {
            //TODO 对象池改造
            // 清空容器中的所有子对象
            foreach (Transform child in container)
            {
                Release(child.gameObject);
            }
        }

        public GameObject GetFromPool()
        {
            return GameObject.Instantiate(buffItemTemplate);
        }

        public void Release(GameObject obj)
        {
            GameObject.Destroy(obj);
        }
    }




}