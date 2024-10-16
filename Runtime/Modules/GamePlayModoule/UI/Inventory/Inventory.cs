using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace XiaoCao
{
	[Serializable]
	public class Inventory
	{
		public List<Item> items = new List<Item>(); // 存储物品的列表

		// 添加物品到背包
		public void AddItem(ItemType itemType, string itemName,  int itemCount,EQuality quality)
		{
			Item newItem = new Item(itemType, itemName, itemCount,quality);
			items.Add(newItem);
			Debug.Log("Added " + newItem.Key + " to inventory. Count: " + newItem.count);
		}

		// 从背包中移除物品
		public void RemoveItem(string itemName)
		{
			Item itemToRemove = items.Find(item => item.Key == itemName);
			if (itemToRemove != null)
			{
				items.Remove(itemToRemove);
			}
		}


		public bool CheckItemAvailability(string itemName, int amount)
		{
			Item item = FindItemByName(itemName);
			return item != null && item.count >= amount;
		}

		// 消耗背包中的物品
		public bool ConsumeItem(string itemName, int amount)
		{
			Item itemToConsume = FindItemByName(itemName);
			if (itemToConsume != null && itemToConsume.count >= amount)
			{
				itemToConsume.count -= amount;
				// 如果数量为零，则从背包中移除该物品
				if (itemToConsume.count == 0)
				{
					RemoveItem(itemName);
				}
				return true;
			}
			else
			{
				return false;
			}
		}

		private Item FindItemByName(string itemName)
		{
			return items.Find(item => item.Key == itemName);
		}

        public int GetItemCount(string itemKey)
        {
			var item = FindItemByName(itemKey);
			if (item != null)
			{
				return item.count;
			}
			else
			{
				return 0;
			}
        }

        // 显示背包中的物品
        public void DisplayInventory()
		{
			Debug.Log("Inventory Contents:");
			foreach (Item item in items)
			{
				Debug.Log("Name: " + item.Key + ", Type: " + item.type + ", Count: " + item.count);
			}
		}
	}

	public enum EQuality{
		White = 0,
		Green = 1,
		Blue = 2,
		Purple = 3,
	}

	public enum EEquipmentSub{
		Weapon = 0,
		Clothes = 1,
		
	}
    
	// 物品类，包含类型、数量和名字属性
	[System.Serializable]
	public class Item{
		public EQuality quality;
		public ItemType type;
		public string id;
		public int count;

		public string Key{
			get{
				return $"{id}_{quality}";
			}
		}

	public Item( ItemType itemType, string itemId, int itemCount, EQuality itemQuality = EQuality.Blue)
		{
			id = itemId;
			type = itemType;
			count = itemCount;
			quality = itemQuality;
		}
        
        
	}

	// 物品类型枚举
	public enum ItemType
	{
		Consumable = 0, //消耗品
		Equipment = 1, //装备
	}

	public enum EquipType
	{
		Weapon, //武器
		Armor //道具
	}
}