using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace XiaoCao{
	
	[CreateAssetMenu(menuName = "SO/SkillTreeSo")]
	public class SkillTreeSo : ScriptableObject{

		public List<SkillSubTree> datas;
	}

	[Serializable]
	public class SkillSubTree: IIndex{
		public int id = 0;
		public int Id => id;
	}
}