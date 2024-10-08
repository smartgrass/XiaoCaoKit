using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace XiaoCao{
	
	[CreateAssetMenu(menuName = "SO/SkillTreeSettingSo")]
	public class SkillTreeSetting : ScriptableObject{
		public List<SkillSubTree> datas;
	}

	[Serializable]
	public class SkillSubTree: IIndex{
		public int id = 0;
		public int Id => id;
		public Vector2 pos;
		public Vector2 link;
		public Sprite icon;
	}
}