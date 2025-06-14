using System;
using XiaoCao;

namespace NaughtyAttributes
{
    //与其他layout无法同时使用, 如HorLayoutAttribute
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class FoldoutAttribute : MetaAttribute, IGroupAttribute, IXCDrawAttribute
	{
		public string Name { get; private set; }

		public FoldoutAttribute(string name)
		{
			Name = name;
		}

        public void OnDraw(UnityEngine.Object targetObject, Action action)
        {
        }
    }
}
