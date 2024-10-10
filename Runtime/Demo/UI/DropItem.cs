namespace XiaoCao
{
    //宝箱掉落
    public class DropItem{
		//实现,可随机,可配置
		public string getFromSetting = "";
		
		public ItemType ItemType;
		public EEquipmentSub EquipmentSub;
		public EQuality itemQuality;
		public int level = 10;
		
		public Item GetItem(){
			if (!string.IsNullOrEmpty( getFromSetting)){
				
			}
			
			
			string id = $"{EquipmentSub.ToString()}_{level}";
			return new Item( ItemType.Equipment,id,1, itemQuality);
		}
	}
}