using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace XiaoCao{
	
	[CreateAssetMenu(menuName = "SO/SkillTreeSo")]
	public class SkillTreeSo : ScriptableObject{
		public Vector2 posScale = Vector2.one;

		public List<SkillSubTree> datas;
	}

	[Serializable]
	public class SkillSubTree: IIndex{
		public int id = 0;
		public int Id => id;
		public Vector2Int pos;
		public Vector2Int link;
		public Sprite icon;
	}
}