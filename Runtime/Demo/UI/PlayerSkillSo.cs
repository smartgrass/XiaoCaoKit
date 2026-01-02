using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace XiaoCao{
	
	[CreateAssetMenu(menuName = "SO/PlayerSkillSo")]
	public class PlayerSkillSo : ScriptableObject{

		public List<string> player0AllSkill;
		
		
		public List<string> GetSkillList(int roleId){
			return player0AllSkill;
		}
	}


}